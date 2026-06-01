using AutoMapper;
using E_Com.Core.DTO;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecommendationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public RecommendationsController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // "Customers who bought this also bought" — collaborative filtering on order history
        [HttpGet("frequently-bought/{productId}")]
        public async Task<IActionResult> FrequentlyBought(int productId, [FromQuery] int count = 4)
        {
            // 1) Find all orders that contain this product
            var orderIdsWithProduct = await _context.Orders
                .Where(o => o.orderItems.Any(i => i.ProductItemId == productId))
                .Select(o => o.Id)
                .ToListAsync();

            if (orderIdsWithProduct.Count == 0)
                return Ok(await FallbackSameCategory(productId, count));

            // 2) Gather co-purchased product ids and rank by frequency
            var coProductIds = await _context.Orders
                .Where(o => orderIdsWithProduct.Contains(o.Id))
                .SelectMany(o => o.orderItems)
                .Where(i => i.ProductItemId != productId)
                .GroupBy(i => i.ProductItemId)
                .Select(g => new { ProductId = g.Key, Freq = g.Count() })
                .OrderByDescending(x => x.Freq)
                .Take(count)
                .ToListAsync();

            if (coProductIds.Count == 0)
                return Ok(await FallbackSameCategory(productId, count));

            var ids = coProductIds.Select(x => x.ProductId).ToList();
            var products = await _context.Products
                .Include(p => p.Photos).Include(p => p.Category)
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            // preserve frequency order
            var ordered = ids
                .Select(id => products.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)
                .ToList();

            return Ok(_mapper.Map<List<ProductDTO>>(ordered));
        }

        // "Recommended for you" — based on the categories the user has purchased from
        [HttpGet("for-user/{email}")]
        public async Task<IActionResult> ForUser(string email, [FromQuery] int count = 6)
        {
            var purchasedProductIds = await _context.Orders
                .Where(o => o.BuyerEmail == email)
                .SelectMany(o => o.orderItems)
                .Select(i => i.ProductItemId)
                .Distinct()
                .ToListAsync();

            if (purchasedProductIds.Count == 0)
            {
                // new user → best sellers
                var top = await _context.Products
                    .Include(p => p.Photos).Include(p => p.Category)
                    .OrderByDescending(p => p.SoldCount).Take(count).ToListAsync();
                return Ok(_mapper.Map<List<ProductDTO>>(top));
            }

            var categoryIds = await _context.Products
                .Where(p => purchasedProductIds.Contains(p.Id))
                .Select(p => p.CategoryId).Distinct().ToListAsync();

            var recs = await _context.Products
                .Include(p => p.Photos).Include(p => p.Category)
                .Where(p => categoryIds.Contains(p.CategoryId) && !purchasedProductIds.Contains(p.Id))
                .OrderByDescending(p => p.SoldCount)
                .Take(count)
                .ToListAsync();

            return Ok(_mapper.Map<List<ProductDTO>>(recs));
        }

        private async Task<List<ProductDTO>> FallbackSameCategory(int productId, int count)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return new List<ProductDTO>();

            var related = await _context.Products
                .Include(p => p.Photos).Include(p => p.Category)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != productId)
                .OrderByDescending(p => p.SoldCount)
                .Take(count)
                .ToListAsync();

            return _mapper.Map<List<ProductDTO>>(related);
        }
    }
}
