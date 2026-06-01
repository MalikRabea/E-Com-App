using E_Com.Core.DTO;
using E_Com.Core.Entites;
using E_Com.Core.Services;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GiftCardsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;

        public GiftCardsController(AppDbContext context, UserManager<AppUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // Buy / issue a gift card
        [HttpPost("purchase")]
        [Authorize]
        public async Task<IActionResult> Purchase([FromBody] PurchaseGiftCardDTO dto)
        {
            if (dto.Amount < 5 || dto.Amount > 1000)
                return BadRequest("Amount must be between $5 and $1000");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var code = GenerateCode();

            var card = new GiftCard
            {
                Code              = code,
                InitialBalance    = dto.Amount,
                CurrentBalance    = dto.Amount,
                IssuedToEmail     = dto.RecipientEmail,
                PurchasedByUserId = user.Id,
                Message           = dto.Message,
                IsActive          = true,
                CreatedAt         = DateTime.UtcNow,
                ExpiryDate        = DateTime.UtcNow.AddYears(1)
            };

            _context.GiftCards.Add(card);
            await _context.SaveChangesAsync();

            // Email the recipient (non-blocking)
            if (!string.IsNullOrEmpty(dto.RecipientEmail))
                _ = SendGiftCardEmail(dto.RecipientEmail, code, dto.Amount, dto.Message, user.DisplayName);

            return Ok(new { card.Code, card.CurrentBalance, card.ExpiryDate });
        }

        // Check balance by code
        [HttpGet("balance/{code}")]
        public async Task<IActionResult> CheckBalance(string code)
        {
            var card = await _context.GiftCards.FirstOrDefaultAsync(c => c.Code == code.ToUpper());
            if (card == null) return NotFound(new { message = "Gift card not found" });

            var valid = card.IsActive && card.CurrentBalance > 0
                        && (card.ExpiryDate == null || card.ExpiryDate > DateTime.UtcNow);

            return Ok(new
            {
                card.Code,
                card.CurrentBalance,
                card.InitialBalance,
                card.ExpiryDate,
                IsValid = valid
            });
        }

        // Redeem (apply) a gift card amount
        [HttpPost("redeem")]
        [Authorize]
        public async Task<IActionResult> Redeem([FromBody] RedeemGiftCardDTO dto)
        {
            var card = await _context.GiftCards.FirstOrDefaultAsync(c => c.Code == dto.Code.ToUpper());
            if (card == null) return NotFound(new { message = "Invalid gift card code" });

            if (!card.IsActive || card.CurrentBalance <= 0)
                return BadRequest(new { message = "Gift card has no balance" });
            if (card.ExpiryDate != null && card.ExpiryDate < DateTime.UtcNow)
                return BadRequest(new { message = "Gift card has expired" });

            var applied = Math.Min(card.CurrentBalance, dto.Amount);
            card.CurrentBalance -= applied;
            if (card.CurrentBalance <= 0) card.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { Applied = applied, RemainingBalance = card.CurrentBalance });
        }

        // My purchased gift cards
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMine()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var cards = await _context.GiftCards
                .Where(c => c.PurchasedByUserId == user.Id)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new { c.Code, c.InitialBalance, c.CurrentBalance, c.IssuedToEmail, c.IsActive, c.CreatedAt, c.ExpiryDate })
                .ToListAsync();

            return Ok(cards);
        }

        private static string GenerateCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rnd = new Random();
            var part = () => new string(Enumerable.Range(0, 4).Select(_ => chars[rnd.Next(chars.Length)]).ToArray());
            return $"GIFT-{part()}-{part()}";
        }

        private async Task SendGiftCardEmail(string to, string code, decimal amount, string? msg, string sender)
        {
            try
            {
                var html = $@"<!DOCTYPE html><html><body style='font-family:Inter,sans-serif;max-width:600px;margin:0 auto;color:#1e293b'>
  <div style='background:linear-gradient(135deg,#2563eb,#7c3aed);padding:32px;border-radius:12px 12px 0 0;text-align:center'>
    <h1 style='color:#fff;margin:0;font-size:1.6rem'>🎁 You received a Gift Card!</h1>
  </div>
  <div style='background:#f8fafc;padding:24px;border-radius:0 0 12px 12px;border:1px solid #e2e8f0;text-align:center'>
    <p><strong>{sender}</strong> sent you an E-Shop gift card</p>
    <div style='font-size:2.5rem;font-weight:900;color:#2563eb;margin:16px 0'>${amount:F2}</div>
    <div style='background:#fff;border:2px dashed #2563eb;border-radius:8px;padding:16px;margin:16px 0'>
      <p style='margin:0;font-size:0.8rem;color:#64748b'>Your code</p>
      <p style='margin:4px 0 0;font-size:1.4rem;font-weight:800;letter-spacing:2px;font-family:monospace'>{code}</p>
    </div>
    {(string.IsNullOrEmpty(msg) ? "" : $"<p style='font-style:italic;color:#475569'>\"{msg}\"</p>")}
    <a href='https://e-com-app-ngx.onrender.com/shop'
       style='display:inline-block;background:#2563eb;color:#fff;padding:12px 28px;border-radius:8px;text-decoration:none;font-weight:700;margin-top:12px'>
      Start Shopping →
    </a>
  </div></body></html>";

                await _emailService.SendEmail(new EmailDTO
                {
                    To      = to,
                    Subject = $"🎁 {sender} sent you a ${amount:F0} E-Shop Gift Card!",
                    Content = html
                });
            }
            catch { }
        }
    }

    public class PurchaseGiftCardDTO
    {
        public decimal Amount         { get; set; }
        public string? RecipientEmail { get; set; }
        public string? Message        { get; set; }
    }

    public class RedeemGiftCardDTO
    {
        public string  Code   { get; set; }
        public decimal Amount { get; set; }
    }
}
