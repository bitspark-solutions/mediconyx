namespace mediconyx_api.Services;

public interface IEmailService
{
    Task SendNewRequestNotificationAsync(int requestId, string hospitalName, string email);
    Task SendStatusChangeNotificationAsync(int requestId, string hospitalName, string email, string oldStatus, string newStatus);
    Task SendClaimedNotificationAsync(int requestId, string hospitalName, string email, string claimedByName);
}
