using E_Com.Core.Entites.Products;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductVariantsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductVariantsController(AppDbContext context) => _context = context;

        [HttpGet("{productId}")]
        public async Task<IActionResult> GetVariants(int productId)
        {
            var variants = await _context.ProductVariants
                .Where(v => v.ProductId == productId)
                .Include(v => v.Options)
                .Select(v => new
                {
                    v.Id, v.Type,
                    Options = v.Options.Select(o => new
                    {
                        o.Id, o.Value, o.Stock, o.PriceAdjustment
                    })
                })
                .ToListAsync();
            return Ok(variants);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddVariant([FromBody] AddVariantDTO dto)
        {
            var variant = new ProductVariant
            {
                ProductId = dto.ProductId,
                Type      = dto.Type,
                Options   = dto.Options.Select(o => new VariantOption
                {
                    Value = o.Value, Stock = o.Stock, PriceAdjustment = o.PriceAdjustment
                }).ToList()
            };
            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();
            return Ok(variant.Id);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVariant(int id)
        {
            var variant = await _context.ProductVariants.FindAsync(id);
            if (variant == null) return NotFound();
            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    public class AddVariantDTO
    {
        public int ProductId { get; set; }
        public string Type { get; set; }
        public List<VariantOptionDTO> Options { get; set; }
    }

    public class VariantOptionDTO
    {
        public string Value { get; set; }
        public int Stock { get; set; }
        public decimal PriceAdjustment { get; set; }
    }
}
