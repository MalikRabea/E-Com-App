using E_Com.Core.Entites.Products;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockReservationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private const int RESERVE_MINUTES = 10;

        public StockReservationController(AppDbContext context) => _context = context;

        // How many units are actually available right now (stock minus active reservations by OTHER baskets)
        [HttpGet("available/{productId}")]
        public async Task<IActionResult> Available(int productId, [FromQuery] string? basketId = null)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var now = DateTime.UtcNow;
            var reservedByOthers = await _context.StockReservations
                .Where(r => r.ProductId == productId && !r.Released && r.ExpiresAt > now
                            && (basketId == null || r.BasketId != basketId))
                .SumAsync(r => (int?)r.Quantity) ?? 0;

            var available = Math.Max(0, product.StockQuantity - reservedByOthers);
            return Ok(new { Available = available, Stock = product.StockQuantity, Reserved = reservedByOthers });
        }

        // Reserve stock for a basket at checkout start (10-minute hold)
        [HttpPost("reserve")]
        public async Task<IActionResult> Reserve([FromBody] ReserveDTO dto)
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null) return NotFound();

            var now = DateTime.UtcNow;

            // release this basket's previous reservation for the product
            var existing = await _context.StockReservations
                .Where(r => r.ProductId == dto.ProductId && r.BasketId == dto.BasketId && !r.Released)
                .ToListAsync();
            existing.ForEach(r => r.Released = true);

            // check availability excluding this basket
            var reservedByOthers = await _context.StockReservations
                .Where(r => r.ProductId == dto.ProductId && !r.Released && r.ExpiresAt > now && r.BasketId != dto.BasketId)
                .SumAsync(r => (int?)r.Quantity) ?? 0;

            var available = product.StockQuantity - reservedByOthers;
            if (dto.Quantity > available)
                return BadRequest(new { message = $"Only {available} left in stock", available });

            _context.StockReservations.Add(new StockReservation
            {
                ProductId = dto.ProductId,
                BasketId  = dto.BasketId,
                Quantity  = dto.Quantity,
                ExpiresAt = now.AddMinutes(RESERVE_MINUTES)
            });
            await _context.SaveChangesAsync();

            return Ok(new { reservedUntil = now.AddMinutes(RESERVE_MINUTES), minutes = RESERVE_MINUTES });
        }

        // Release a basket's reservations (e.g. when leaving checkout)
        [HttpPost("release")]
        public async Task<IActionResult> Release([FromBody] ReleaseDTO dto)
        {
            var items = await _context.StockReservations
                .Where(r => r.BasketId == dto.BasketId && !r.Released)
                .ToListAsync();
            items.ForEach(r => r.Released = true);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    public class ReserveDTO
    {
        public int    ProductId { get; set; }
        public string BasketId  { get; set; }
        public int    Quantity  { get; set; }
    }

    public class ReleaseDTO { public string BasketId { get; set; } }
}
