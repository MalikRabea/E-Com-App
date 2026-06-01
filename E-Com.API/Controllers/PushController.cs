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
    public class PushController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _config;

        public PushController(AppDbContext context, UserManager<AppUser> userManager, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _config = config;
        }

        [HttpGet("vapid-public-key")]
        public IActionResult GetVapidPublicKey()
        {
            var key = _config["Push:VapidPublicKey"] ?? "";
            return Ok(new { publicKey = key });
        }

        [HttpPost("subscribe")]
        [Authorize]
        public async Task<IActionResult> Subscribe([FromBody] PushSubscribeDTO dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var existing = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.Endpoint == dto.Endpoint);

            if (existing == null)
            {
                _context.PushSubscriptions.Add(new PushSubscription
                {
                    UserId   = user.Id,
                    Endpoint = dto.Endpoint,
                    P256dh   = dto.P256dh,
                    Auth     = dto.Auth
                });
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpDelete("unsubscribe")]
        [Authorize]
        public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeDTO dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var sub = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.Endpoint == dto.Endpoint);

            if (sub != null)
            {
                _context.PushSubscriptions.Remove(sub);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
    }

    public class PushSubscribeDTO
    {
        public string Endpoint { get; set; }
        public string P256dh   { get; set; }
        public string Auth     { get; set; }
    }

    public class UnsubscribeDTO { public string Endpoint { get; set; } }
}
