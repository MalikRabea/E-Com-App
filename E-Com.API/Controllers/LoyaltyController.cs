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
    public class LoyaltyController : ControllerBase
    {
        private readonly ILoyaltyService _loyaltyService;
        private readonly UserManager<AppUser> _userManager;

        public LoyaltyController(ILoyaltyService loyaltyService, UserManager<AppUser> userManager)
        {
            _loyaltyService = loyaltyService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyPoints()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var account = await _loyaltyService.GetOrCreateAccountAsync(user.Id);
            var history = await _loyaltyService.GetHistoryAsync(user.Id);

            return Ok(new
            {
                account.Points,
                account.Tier,
                NextTier     = GetNextTier(account.Tier),
                PointsToNext = GetPointsToNext(account.Points),
                History      = history.Select(t => new
                {
                    t.Points, t.Description, t.OrderId,
                    Type      = t.Type.ToString(),
                    CreatedAt = t.CreatedAt
                })
            });
        }

        [HttpPost("redeem")]
        public async Task<IActionResult> Redeem([FromBody] RedeemDTO dto)
        {
            if (dto.Points <= 0) return BadRequest("Points must be positive");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var success = await _loyaltyService.RedeemPointsAsync(user.Id, dto.Points);
            if (!success) return BadRequest("Insufficient points");

            var discount = dto.Points / 100m; // 100 points = $1
            return Ok(new { Discount = discount, Message = $"Redeemed {dto.Points} points for ${discount:F2} discount" });
        }

        private static string GetNextTier(string current) => current switch
        {
            "Bronze"   => "Silver (500 pts)",
            "Silver"   => "Gold (2000 pts)",
            "Gold"     => "Platinum (5000 pts)",
            "Platinum" => "Max tier reached!",
            _          => "Silver"
        };

        private static int GetPointsToNext(int points) => points switch
        {
            < 500  => 500  - points,
            < 2000 => 2000 - points,
            < 5000 => 5000 - points,
            _      => 0
        };
    }

    public class RedeemDTO
    {
        public int Points { get; set; }
    }
}
