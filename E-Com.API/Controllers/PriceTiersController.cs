using E_Com.Core.Entites.Products;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PriceTiersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public PriceTiersController(AppDbContext context) => _context = context;

        // Public: tiers for a product (with base price as the first row)
        [HttpGet("{productId}")]
        public async Task<IActionResult> GetForProduct(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var tiers = await _context.PriceTiers
                .Where(t => t.ProductId == productId)
                .OrderBy(t => t.MinQuantity)
                .Select(t => new { t.Id, t.MinQuantity, t.UnitPrice })
                .ToListAsync();

            return Ok(new { BasePrice = product.NewPrice, Tiers = tiers });
        }

        // Compute the effective unit price for a given quantity
        [HttpGet("{productId}/price")]
        public async Task<IActionResult> PriceForQty(int productId, [FromQuery] int qty = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var tier = await _context.PriceTiers
                .Where(t => t.ProductId == productId && t.MinQuantity <= qty)
                .OrderByDescending(t => t.MinQuantity)
                .FirstOrDefaultAsync();

            var unit = tier?.UnitPrice ?? product.NewPrice;
            return Ok(new { UnitPrice = unit, Total = unit * qty, BasePrice = product.NewPrice });
        }

        // ── Admin ──
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add([FromBody] AddTierDTO dto)
        {
            _context.PriceTiers.Add(new PriceTier
            {
                ProductId = dto.ProductId, MinQuantity = dto.MinQuantity, UnitPrice = dto.UnitPrice
            });
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var t = await _context.PriceTiers.FindAsync(id);
            if (t == null) return NotFound();
            _context.PriceTiers.Remove(t);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    public class AddTierDTO
    {
        public int     ProductId   { get; set; }
        public int     MinQuantity { get; set; }
        public decimal UnitPrice   { get; set; }
    }
}
