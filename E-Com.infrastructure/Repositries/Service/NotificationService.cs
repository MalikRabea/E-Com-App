using E_Com.Core.Entites;
using E_Com.Core.Entites.Notifications;
using E_Com.Core.Services;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E_Com.infrastructure.Repositries.Service
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public NotificationService(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task NotifyUserAsync(string userId, string type, string icon, string title, string message, string? link = null)
        {
            if (string.IsNullOrEmpty(userId)) return;
            _context.Notifications.Add(new Notification
            {
                UserId = userId, Type = type, Icon = icon, Title = title, Message = message, Link = link
            });
            await _context.SaveChangesAsync();
        }

        public async Task NotifyByEmailAsync(string email, string type, string icon, string title, string message, string? link = null)
        {
            if (string.IsNullOrEmpty(email)) return;
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
                await NotifyUserAsync(user.Id, type, icon, title, message, link);
        }

        public async Task NotifyAdminsAsync(string type, string icon, string title, string message, string? link = null)
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id, Type = type, Icon = icon, Title = title, Message = message, Link = link
                });
            }
            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<Notification>> GetForUserAsync(string userId, int take = 30)
            => await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();

        public async Task<int> UnreadCountAsync(string userId)
            => await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

        public async Task MarkReadAsync(int id, string userId)
        {
            var n = await _context.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (n != null) { n.IsRead = true; await _context.SaveChangesAsync(); }
        }

        public async Task MarkAllReadAsync(string userId)
        {
            var list = await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            list.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();
        }
    }
}
