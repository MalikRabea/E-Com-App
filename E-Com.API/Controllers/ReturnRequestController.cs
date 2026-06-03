using E_Com.Core.Entites;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReturnRequestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly E_Com.Core.Services.INotificationService _notifications;

        public ReturnRequestController(AppDbContext context, UserManager<AppUser> userManager,
            E_Com.Core.Services.INotificationService notifications)
        {
            _context = context;
            _userManager = userManager;
            _notifications = notifications;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReturnDTO dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var request = new ReturnRequest
            {
                UserId      = user.Id,
                UserEmail   = user.Email!,
                OrderId     = dto.OrderId,
                Reason      = dto.Reason,
                Description = dto.Description ?? "",
                Status      = "Pending",
                CreatedAt   = DateTime.UtcNow
            };

            _context.ReturnRequests.Add(request);
            await _context.SaveChangesAsync();

            // Notify admins of the new return request
            await _notifications.NotifyAdminsAsync("warning", "assignment_return",
                "New Return Request",
                $"Order #{dto.OrderId} · {dto.Reason} — from {user.Email}",
                "/admin/returns");

            return Ok(new { request.Id, request.Status, Message = "Return request submitted successfully." });
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMine()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var requests = await _context.ReturnRequests
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id, r.OrderId, r.Reason, r.Description,
                    r.Status, r.AdminNote, r.CreatedAt, r.UpdatedAt
                })
                .ToListAsync();

            return Ok(requests);
        }
    }

    public class CreateReturnDTO
    {
        public int     OrderId     { get; set; }
        public string  Reason      { get; set; }
        public string? Description { get; set; }
    }
}
