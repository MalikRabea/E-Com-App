using E_Com.Core.Entites;
using E_Com.Core.Entites.Support;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupportController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly E_Com.Core.Services.INotificationService _notifications;

        public SupportController(AppDbContext context, UserManager<AppUser> userManager,
            E_Com.Core.Services.INotificationService notifications)
        {
            _context = context;
            _userManager = userManager;
            _notifications = notifications;
        }

        // ── User: create ticket ──
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateTicketDTO dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var ticket = new SupportTicket
            {
                UserId    = user.Id,
                UserEmail = user.Email!,
                Subject   = dto.Subject,
                Category  = dto.Category,
                Status    = "Open",
                Priority  = "Normal",
                Messages  = new List<TicketMessage>
                {
                    new TicketMessage { SenderId = user.Id, IsAdmin = false, Body = dto.Message }
                }
            };
            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Notify admins of the new ticket
            await _notifications.NotifyAdminsAsync("support", "support_agent",
                "New Support Ticket",
                $"#{ticket.Id} · {dto.Subject} — from {user.Email}",
                "/admin/support");

            return Ok(new { ticket.Id });
        }

        // ── User: my tickets ──
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMine()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var tickets = await _context.SupportTickets
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.UpdatedAt)
                .Select(t => new { t.Id, t.Subject, t.Category, t.Status, t.Priority, t.CreatedAt, t.UpdatedAt,
                    MessageCount = t.Messages.Count })
                .ToListAsync();
            return Ok(tickets);
        }

        // ── Get ticket thread (owner or admin) ──
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetThread(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var ticket = await _context.SupportTickets
                .Include(t => t.Messages)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();
            if (!isAdmin && ticket.UserId != user.Id) return Forbid();

            return Ok(new
            {
                ticket.Id, ticket.Subject, ticket.Category, ticket.Status, ticket.Priority, ticket.UserEmail, ticket.CreatedAt,
                Messages = ticket.Messages.OrderBy(m => m.CreatedAt)
                    .Select(m => new { m.Id, m.Body, m.IsAdmin, m.CreatedAt })
            });
        }

        // ── Reply (owner or admin) ──
        [HttpPost("{id}/reply")]
        [Authorize]
        public async Task<IActionResult> Reply(int id, [FromBody] ReplyDTO dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket == null) return NotFound();
            if (!isAdmin && ticket.UserId != user.Id) return Forbid();

            _context.TicketMessages.Add(new TicketMessage
            {
                TicketId = id, SenderId = user.Id, IsAdmin = isAdmin, Body = dto.Body
            });
            ticket.UpdatedAt = DateTime.UtcNow;
            if (isAdmin && ticket.Status == "Open") ticket.Status = "Pending";
            await _context.SaveChangesAsync();

            // Notify the other party
            if (isAdmin)
                await _notifications.NotifyUserAsync(ticket.UserId, "support", "support_agent",
                    "Support replied to your ticket",
                    $"#{ticket.Id} · {ticket.Subject}", "/help");
            else
                await _notifications.NotifyAdminsAsync("support", "support_agent",
                    "Customer replied to a ticket",
                    $"#{ticket.Id} · {ticket.Subject}", "/admin/support");

            return Ok();
        }

        // ── Admin: all tickets ──
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AllTickets([FromQuery] string? status = null)
        {
            var query = _context.SupportTickets.AsQueryable();
            if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);

            var tickets = await query
                .OrderByDescending(t => t.UpdatedAt)
                .Select(t => new { t.Id, t.Subject, t.Category, t.Status, t.Priority, t.UserEmail, t.CreatedAt, t.UpdatedAt,
                    MessageCount = t.Messages.Count })
                .ToListAsync();
            return Ok(tickets);
        }

        // ── Admin: change status ──
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetStatus(int id, [FromBody] TicketStatusDTO dto)
        {
            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket == null) return NotFound();
            ticket.Status = dto.Status;
            ticket.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    public class CreateTicketDTO { public string Subject { get; set; } public string Category { get; set; } public string Message { get; set; } }
    public class ReplyDTO        { public string Body { get; set; } }
    public class TicketStatusDTO { public string Status { get; set; } }
}
