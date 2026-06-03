using E_Com.Core.Entites.Products;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BundlesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public BundlesController(AppDbContext context) => _context = context;

        // Public: active bundles with computed pricing
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var bundles = await _context.Bundles
                .Where(b => b.IsActive)
                .Include(b => b.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Photos)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return Ok(bundles.Select(b => MapBundle(b)));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var b = await _context.Bundles
                .Include(x => x.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Photos)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (b == null) return NotFound();
            return Ok(MapBundle(b));
        }

        // Bundles that include a given product (shown on product page)
        [HttpGet("for-product/{productId}")]
        public async Task<IActionResult> ForProduct(int productId)
        {
            var bundles = await _context.Bundles
                .Where(b => b.IsActive && b.Items.Any(i => i.ProductId == productId))
                .Include(b => b.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Photos)
                .Take(3)
                .ToListAsync();
            return Ok(bundles.Select(b => MapBundle(b)));
        }

        // ── Admin ──
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAdmin()
        {
            var bundles = await _context.Bundles
                .Include(b => b.Items).ThenInclude(i => i.Product)
                .OrderByDescending(b => b.CreatedAt).ToListAsync();
            return Ok(bundles.Select(b => MapBundle(b)));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateBundleDTO dto)
        {
            var bundle = new Bundle
            {
                Name = dto.Name, Description = dto.Description ?? "",
                DiscountPercent = dto.DiscountPercent, IsActive = true,
                Items = dto.ProductIds.Select(pid => new BundleItem { ProductId = pid }).ToList()
            };
            _context.Bundles.Add(bundle);
            await _context.SaveChangesAsync();
            return Ok(bundle.Id);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var b = await _context.Bundles.FindAsync(id);
            if (b == null) return NotFound();
            _context.Bundles.Remove(b);
            await _context.SaveChangesAsync();
            return Ok();
        }

        private static object MapBundle(Bundle b)
        {
            var original = b.Items.Sum(i => i.Product?.NewPrice ?? 0);
            var bundlePrice = Math.Round(original * (1 - b.DiscountPercent / 100m), 2);
            return new
            {
                b.Id, b.Name, b.Description, b.DiscountPercent,
                OriginalPrice = original,
                BundlePrice   = bundlePrice,
                Savings       = Math.Round(original - bundlePrice, 2),
                Items = b.Items.Select(i => new
                {
                    i.ProductId,
                    Name  = i.Product?.Name,
                    Price = i.Product?.NewPrice,
                    Image = i.Product?.Photos?.Select(p => p.ImageName).FirstOrDefault() ?? ""
                })
            };
        }
    }

    public class CreateBundleDTO
    {
        public string  Name            { get; set; }
        public string? Description     { get; set; }
        public decimal DiscountPercent { get; set; } = 10;
        public List<int> ProductIds    { get; set; } = new();
    }
}
