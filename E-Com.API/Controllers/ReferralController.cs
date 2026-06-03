using E_Com.Core.Entites;
using E_Com.Core.Entites.Marketing;
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
    public class ReferralController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILoyaltyService _loyalty;
        private readonly INotificationService _notifications;

        private const int REFERRER_REWARD = 500; // points
        private const int REFERRED_REWARD = 250;

        public ReferralController(AppDbContext context, UserManager<AppUser> userManager,
            ILoyaltyService loyalty, INotificationService notifications)
        {
            _context = context;
            _userManager = userManager;
            _loyalty = loyalty;
            _notifications = notifications;
        }

        // Get (or create) my referral profile + stats
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMine()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var profile = await GetOrCreateProfile(user.Id);
            var referrals = await _context.Referrals
                .Where(r => r.ReferrerUserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new { r.ReferredEmail, r.Status, r.CreatedAt, r.CompletedAt })
                .ToListAsync();

            return Ok(new
            {
                profile.Code,
                profile.TotalReferred,
                profile.PointsEarned,
                ReferrerReward = REFERRER_REWARD,
                ReferredReward = REFERRED_REWARD,
                Referrals = referrals
            });
        }

        // Record that someone signed up with a referral code (called on registration)
        [HttpPost("track")]
        public async Task<IActionResult> Track([FromBody] TrackReferralDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code)) return Ok();

            var profile = await _context.ReferralProfiles.FirstOrDefaultAsync(p => p.Code == dto.Code.ToUpper());
            if (profile == null) return Ok(); // silently ignore invalid codes

            var exists = await _context.Referrals.AnyAsync(r => r.ReferredEmail == dto.Email);
            if (exists) return Ok();

            _context.Referrals.Add(new Referral
            {
                ReferrerUserId = profile.UserId,
                ReferredEmail  = dto.Email,
                Code           = dto.Code.ToUpper(),
                Status         = "Pending"
            });
            await _context.SaveChangesAsync();
            return Ok();
        }

        // Complete a referral when the referred user places their first order — rewards both sides
        [HttpPost("complete")]
        [Authorize]
        public async Task<IActionResult> Complete()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var referral = await _context.Referrals
                .FirstOrDefaultAsync(r => r.ReferredEmail == user.Email && r.Status == "Pending");
            if (referral == null) return Ok(new { rewarded = false });

            referral.Status = "Completed";
            referral.CompletedAt = DateTime.UtcNow;

            var profile = await _context.ReferralProfiles.FirstOrDefaultAsync(p => p.UserId == referral.ReferrerUserId);
            if (profile != null)
            {
                profile.TotalReferred += 1;
                profile.PointsEarned  += REFERRER_REWARD;
                await _loyalty.AwardPointsAsync(referral.ReferrerUserId, REFERRER_REWARD, $"Referral reward — {user.Email}");
                await _notifications.NotifyUserAsync(referral.ReferrerUserId, "success", "group_add",
                    "Referral Reward Earned! 🎉",
                    $"{user.Email} joined using your code. You earned {REFERRER_REWARD} points!",
                    "/Account/referral");
            }

            await _loyalty.AwardPointsAsync(user.Id, REFERRED_REWARD, "Welcome referral bonus");
            await _notifications.NotifyUserAsync(user.Id, "success", "redeem",
                "Welcome Bonus! 🎁",
                $"You earned {REFERRED_REWARD} points for joining with a referral code.",
                "/Account/loyalty");
            await _context.SaveChangesAsync();

            return Ok(new { rewarded = true, referredReward = REFERRED_REWARD });
        }

        private async Task<ReferralProfile> GetOrCreateProfile(string userId)
        {
            var profile = await _context.ReferralProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                profile = new ReferralProfile { UserId = userId, Code = GenerateCode() };
                _context.ReferralProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }
            return profile;
        }

        private static string GenerateCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rnd = new Random();
            return "REF" + new string(Enumerable.Range(0, 6).Select(_ => chars[rnd.Next(chars.Length)]).ToArray());
        }
    }

    public class TrackReferralDTO { public string Code { get; set; } public string Email { get; set; } }
}
