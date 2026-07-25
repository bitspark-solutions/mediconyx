namespace mediconyx_api.Services.AI;

public class StorageServiceOptions
{
    public string BaseUrl { get; set; } = "http://localhost:7890";

    /// <summary>Shared secret sent to the storage service as X-Api-Key (must match Storage:ApiKey there).</summary>
    public string ApiKey { get; set; } = string.Empty;
}
