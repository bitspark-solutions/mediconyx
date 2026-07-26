namespace mediconyx_api.Services.Infrastructure;

/// <summary>
/// Generic email service for sending any kind of email.
/// Domain-specific services build their own subject/body and call <see cref="SendEmailAsync"/>.
/// </summary>
public interface IEmailService
{
    /// <summary>Send an email. The body may be HTML.</summary>
    Task SendEmailAsync(string to, string subject, string body);
}
