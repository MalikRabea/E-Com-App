using E_Com.Core.Entites.Notifications;

namespace E_Com.Core.Services
{
    public interface INotificationService
    {
        Task NotifyUserAsync(string userId, string type, string icon, string title, string message, string? link = null);
        Task NotifyByEmailAsync(string email, string type, string icon, string title, string message, string? link = null);
        Task NotifyAdminsAsync(string type, string icon, string title, string message, string? link = null);
        Task<IReadOnlyList<Notification>> GetForUserAsync(string userId, int take = 30);
        Task<int> UnreadCountAsync(string userId);
        Task MarkReadAsync(int id, string userId);
        Task MarkAllReadAsync(string userId);
    }
}
