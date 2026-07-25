using System.Text.Json;
using Microsoft.Extensions.Options;
using storage_service.Configuration;
using storage_service.Models;

namespace storage_service.Services;

/// <summary>
/// Periodically hard-deletes files whose soft-delete retention has expired:
/// any *.meta.json with IsDeleted = true and DeletedAt older than
/// Storage:SoftDeleteRetentionDays has its blob and sidecar removed.
/// Runs every Storage:PurgeIntervalHours (default 6).
/// </summary>
public class SoftDeletePurgeService : BackgroundService
{
    private readonly StorageOptions _options;
    private readonly ILogger<SoftDeletePurgeService> _logger;

    public SoftDeletePurgeService(IOptions<StorageOptions> options, ILogger<SoftDeletePurgeService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(Math.Max(1, _options.PurgeIntervalHours));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                PurgeExpired();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Soft-delete purge run failed");
            }
            await Task.Delay(interval, stoppingToken);
        }
    }

    private void PurgeExpired()
    {
        if (!Directory.Exists(_options.BasePath)) return;

        var cutoff = DateTime.UtcNow.AddDays(-_options.SoftDeleteRetentionDays);
        var purged = 0;

        foreach (var metaPath in Directory.GetFiles(_options.BasePath, "*.meta.json", SearchOption.AllDirectories))
        {
            try
            {
                var metadata = JsonSerializer.Deserialize<FileMetadata>(File.ReadAllText(metaPath));
                if (metadata?.IsDeleted != true || metadata.DeletedAt == null || metadata.DeletedAt > cutoff)
                    continue;

                var blobPath = metaPath[..^".meta.json".Length] + Path.GetExtension(metadata.FileName);
                if (File.Exists(blobPath)) File.Delete(blobPath);
                File.Delete(metaPath);
                purged++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to purge {MetaPath}", metaPath);
            }
        }

        if (purged > 0)
            _logger.LogInformation("Soft-delete purge: removed {Count} file(s) older than {Days} days", purged, _options.SoftDeleteRetentionDays);
    }
}
