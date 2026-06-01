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
    public class SubscriptionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public SubscriptionsController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubscriptionDTO dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null) return NotFound("Product not found");

            var sub = new Subscription
            {
                UserId           = user.Id,
                UserEmail        = user.Email!,
                ProductId        = dto.ProductId,
                Quantity         = dto.Quantity < 1 ? 1 : dto.Quantity,
                Interval         = dto.Interval,
                DiscountPercent  = 10,
                NextDeliveryDate = CalcNextDate(dto.Interval, DateTime.UtcNow),
                IsActive         = true,
                CreatedAt        = DateTime.UtcNow
            };

            _context.Subscriptions.Add(sub);
            await _context.SaveChangesAsync();
            return Ok(new { sub.Id, sub.NextDeliveryDate });
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMine()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var subs = await _context.Subscriptions
                .Where(s => s.UserId == user.Id)
                .Include(s => s.Product).ThenInclude(p => p.Photos)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.Id, s.ProductId,
                    ProductName  = s.Product.Name,
                    ProductImage = s.Product.Photos.Select(p => p.ImageName).FirstOrDefault() ?? "",
                    UnitPrice    = s.Product.NewPrice,
                    s.Quantity, s.Interval, s.DiscountPercent,
                    s.NextDeliveryDate, s.IsActive, s.CreatedAt
                })
                .ToListAsync();

            return Ok(subs);
        }

        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> Toggle(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == user!.Id);
            if (sub == null) return NotFound();

            sub.IsActive = !sub.IsActive;
            if (sub.IsActive) sub.NextDeliveryDate = CalcNextDate(sub.Interval, DateTime.UtcNow);
            await _context.SaveChangesAsync();
            return Ok(new { sub.IsActive });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == user!.Id);
            if (sub == null) return NotFound();

            _context.Subscriptions.Remove(sub);
            await _context.SaveChangesAsync();
            return Ok();
        }

        public static DateTime CalcNextDate(string interval, DateTime from) => interval switch
        {
            "Weekly"    => from.AddDays(7),
            "Quarterly" => from.AddMonths(3),
            _           => from.AddMonths(1),
        };
    }

    public class CreateSubscriptionDTO
    {
        public int    ProductId { get; set; }
        public int    Quantity  { get; set; } = 1;
        public string Interval  { get; set; } = "Monthly";
    }
}
