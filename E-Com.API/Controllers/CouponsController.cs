using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CouponsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CouponsController(AppDbContext context) { _context = context; }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidateCouponDTO dto)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToLower() == dto.Code.ToLower() && c.IsActive);

            if (coupon == null)
                return BadRequest(new { message = "Invalid coupon code" });
            if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < DateTime.UtcNow)
                return BadRequest(new { message = "Coupon has expired" });
            if (coupon.CurrentUses >= coupon.MaxUses)
                return BadRequest(new { message = "Coupon usage limit reached" });

            return Ok(new
            {
                code            = coupon.Code,
                discountPercent = coupon.DiscountPercent,
                message         = $"{coupon.DiscountPercent}% discount applied!"
            });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var coupons = await _context.Coupons.ToListAsync();
            return Ok(coupons);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCouponDTO dto)
        {
            var coupon = new E_Com.Core.Entites.Coupon
            {
                Code            = dto.Code.ToUpper(),
                DiscountPercent = dto.DiscountPercent,
                MaxUses         = dto.MaxUses,
                ExpiryDate      = dto.ExpiryDate,
                IsActive        = true
            };
            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();
            return Ok(coupon);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();
            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    public record ValidateCouponDTO { public string Code { get; set; } = ""; }
    public record CreateCouponDTO
    {
        public string Code { get; set; } = "";
        public decimal DiscountPercent { get; set; }
        public int MaxUses { get; set; } = 100;
        public DateTime? ExpiryDate { get; set; }
    }
}
