using System.Net;
using System.Net.Mail;

namespace mediconyx_api.Services.Infrastructure;

/// <summary>
/// Generic SMTP email sender. All domain-specific email content is built by the callers.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var smtpHost = _configuration["Smtp:Host"];
            var smtpPort = _configuration.GetValue<int>("Smtp:Port", 587);
            var smtpUsername = _configuration["Smtp:Username"];
            var smtpPassword = _configuration["Smtp:Password"];
            var fromAddress = _configuration["Smtp:FromAddress"] ?? "noreply@mediconyx.com";
            var fromName = _configuration["Smtp:FromName"] ?? "Mediconyx System";

            if (string.IsNullOrWhiteSpace(smtpHost))
            {
                _logger.LogWarning("SMTP host not configured, email not sent to {To}", to);
                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(to);

            using var client = new SmtpClient(smtpHost, smtpPort);
            if (!string.IsNullOrWhiteSpace(smtpUsername) && !string.IsNullOrWhiteSpace(smtpPassword))
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            client.EnableSsl = _configuration.GetValue<bool>("Smtp:EnableSsl", true);
            await client.SendMailAsync(message);

            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
        }
    }
}
