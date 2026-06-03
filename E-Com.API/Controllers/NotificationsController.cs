using E_Com.Core.Entites;
using E_Com.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notifications;
        private readonly UserManager<AppUser> _userManager;

        public NotificationsController(INotificationService notifications, UserManager<AppUser> userManager)
        {
            _notifications = notifications;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var items = await _notifications.GetForUserAsync(user.Id);
            return Ok(items.Select(n => new { n.Id, n.Type, n.Icon, n.Title, n.Message, n.Link, n.IsRead, n.CreatedAt }));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            return Ok(new { count = await _notifications.UnreadCountAsync(user.Id) });
        }

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            await _notifications.MarkReadAsync(id, user.Id);
            return Ok();
        }

        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            await _notifications.MarkAllReadAsync(user.Id);
            return Ok();
        }
    }
}
