using System.Threading.Tasks;

namespace TutorHubBD.Web.Services
{
    /// <summary>
    /// Service for sending notifications to users via Email, SMS, and In-App.
    /// </summary>
    public interface INotificationService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendSmsAsync(string phoneNumber, string message);
        Task SendInAppNotificationAsync(string userId, string title, string message, string? relatedLink = null);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
    }
}
