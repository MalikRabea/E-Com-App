using E_Com.Core.DTO;
using E_Com.Core.Entites;
using E_Com.Core.Entites.Security;
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
    public class TwoFactorController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IGenerateToken _tokenService;

        public TwoFactorController(AppDbContext context, UserManager<AppUser> userManager,
            IEmailService emailService, IGenerateToken tokenService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _tokenService = tokenService;
        }

        // Enable/disable 2FA for the signed-in user
        [HttpPost("toggle")]
        [Authorize]
        public async Task<IActionResult> Toggle()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            user.TwoFactorEnabled = !user.TwoFactorEnabled;
            await _userManager.UpdateAsync(user);
            return Ok(new { enabled = user.TwoFactorEnabled });
        }

        [HttpGet("status")]
        [Authorize]
        public async Task<IActionResult> Status()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            return Ok(new { enabled = user.TwoFactorEnabled });
        }

        // Step 1 of 2FA login: validate password then send OTP (called by frontend after normal login detects 2FA)
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return NotFound();

            // invalidate previous codes
            var old = await _context.OtpCodes.Where(o => o.Email == dto.Email && !o.Used).ToListAsync();
            old.ForEach(o => o.Used = true);

            var code = new Random().Next(100000, 999999).ToString();
            _context.OtpCodes.Add(new OtpCode
            {
                Email     = dto.Email,
                Code      = code,
                Purpose   = "Login",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            });
            await _context.SaveChangesAsync();

            _ = SendOtpEmail(dto.Email, code);
            return Ok(new { message = "OTP sent to your email" });
        }

        // Step 2: verify OTP, return token
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDTO dto)
        {
            var otp = await _context.OtpCodes
                .Where(o => o.Email == dto.Email && o.Code == dto.Code && !o.Used)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null) return BadRequest(new { message = "Invalid code" });
            if (otp.ExpiresAt < DateTime.UtcNow) return BadRequest(new { message = "Code expired" });

            otp.Used = true;
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenService.GetAndCreateToken(user, roles);

            Response.Cookies.Append("token", token, new CookieOptions
            {
                HttpOnly = true, Secure = true, SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(1)
            });

            return Ok(new { user.DisplayName, user.Email });
        }

        private async Task SendOtpEmail(string email, string code)
        {
            try
            {
                var html = $@"<!DOCTYPE html><html><body style='font-family:Inter,sans-serif;max-width:600px;margin:0 auto;color:#1e293b'>
  <div style='background:#2563eb;padding:24px;border-radius:12px 12px 0 0;text-align:center'>
    <h1 style='color:#fff;margin:0'>🔐 Your Login Code</h1>
  </div>
  <div style='background:#f8fafc;padding:24px;border-radius:0 0 12px 12px;border:1px solid #e2e8f0;text-align:center'>
    <p>Use this code to complete your sign-in:</p>
    <div style='font-size:2.5rem;font-weight:900;letter-spacing:8px;color:#2563eb;margin:16px 0'>{code}</div>
    <p style='color:#64748b;font-size:0.85rem'>This code expires in 10 minutes. If you didn't request it, ignore this email.</p>
  </div></body></html>";
                await _emailService.SendEmail(new EmailDTO { To = email, Subject = "Your E-Shop login code", Content = html });
            }
            catch { }
        }
    }

    public class SendOtpDTO   { public string Email { get; set; } }
    public class VerifyOtpDTO { public string Email { get; set; } public string Code { get; set; } }
}
