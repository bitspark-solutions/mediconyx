using System.Net;
using System.Net.Mail;

namespace mediconyx_api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly bool _enabled;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _enabled = _configuration.GetValue<bool>("Notifications:Enabled", true);
    }

    public async Task SendNewRequestNotificationAsync(int requestId, string hospitalName, string email)
    {
        if (!_enabled || !_configuration.GetValue<bool>("Notifications:NotifyOnNewRequest", true))
        {
            _logger.LogDebug("Email notifications disabled, skipping new request notification");
            return;
        }

        var subject = $"New Sales Request from {hospitalName}";
        var body = $@"
<html>
<body>
<h2>New Sales Request</h2>
<p>A new sales request has been submitted:</p>
<ul>
<li><strong>Hospital:</strong> {hospitalName}</li>
<li><strong>Email:</strong> {email}</li>
<li><strong>Request ID:</strong> {requestId}</li>
</ul>
<p>View and claim this request in the portal:
<a href='{_configuration["FrontendUrl"]}/portal/sales'>Sales Dashboard</a>
</p>
<p>Best regards,<br/>Mediconyx System</p>
</body>
</html>";

        var salesTeamEmail = _configuration["Notifications:SalesTeamEmail"];
        if (!string.IsNullOrWhiteSpace(salesTeamEmail))
        {
            await SendEmailAsync(salesTeamEmail, subject, body);
        }
    }

    public async Task SendStatusChangeNotificationAsync(int requestId, string hospitalName, string email, string oldStatus, string newStatus)
    {
        if (!_enabled || !_configuration.GetValue<bool>("Notifications:NotifyOnStatusChange", true))
        {
            _logger.LogDebug("Email notifications disabled, skipping status change notification");
            return;
        }

        var subject = $"Your Mediconyx Request - Status Update";
        var body = $@"
<html>
<body>
<h2>Request Status Updated</h2>
<p>Dear Customer,</p>
<p>Your sales request for <strong>{hospitalName}</strong> has been updated:</p>
<ul>
<li><strong>Previous Status:</strong> {oldStatus}</li>
<li><strong>New Status:</strong> {newStatus}</li>
<li><strong>Request ID:</strong> {requestId}</li>
</ul>";

        if (newStatus == "Active")
        {
            body += $@"
<p><strong>Congratulations!</strong> Your account is now active. You can login here:</p>
<p><a href='{_configuration["FrontendUrl"]}/login'>Login to Mediconyx</a></p>";
        }

        body += @"
<p>If you have questions, please contact our sales team.</p>
<p>Best regards,<br/>Mediconyx Team</p>
</body>
</html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendClaimedNotificationAsync(int requestId, string hospitalName, string email, string claimedByName)
    {
        if (!_enabled || !_configuration.GetValue<bool>("Notifications:NotifyOnClaim", false))
        {
            _logger.LogDebug("Email notifications for claim disabled, skipping");
            return;
        }

        var subject = $"Your Mediconyx Request - Assigned to Sales Representative";
        var body = $@"
<html>
<body>
<h2>Request Assigned</h2>
<p>Dear Customer,</p>
<p>Your sales request for <strong>{hospitalName}</strong> has been assigned to {claimedByName}.</p>
<p>They will be in contact with you shortly.</p>
<p>Request ID: {requestId}</p>
<p>Best regards,<br/>Mediconyx Team</p>
</body>
</html>";

        await SendEmailAsync(email, subject, body);
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
            {
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
            }

            client.EnableSsl = _configuration.GetValue<bool>("Smtp:EnableSsl", true);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
            // Don't throw - email failures shouldn't break the request
        }
    }
}
