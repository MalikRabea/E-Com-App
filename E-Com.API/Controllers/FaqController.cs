using E_Com.Core.Entites.Support;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaqController : ControllerBase
    {
        private readonly AppDbContext _context;
        public FaqController(AppDbContext context) => _context = context;

        // Public: active FAQs
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var faqs = await _context.FaqItems
                .Where(f => f.IsActive)
                .OrderBy(f => f.Category).ThenBy(f => f.SortOrder)
                .Select(f => new { f.Id, f.Question, f.Answer, f.Category })
                .ToListAsync();
            return Ok(faqs);
        }

        // Admin
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAdmin()
            => Ok(await _context.FaqItems.OrderBy(f => f.SortOrder).ToListAsync());

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] FaqItem item)
        {
            _context.FaqItems.Add(item);
            await _context.SaveChangesAsync();
            return Ok(item.Id);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] FaqItem item)
        {
            var faq = await _context.FaqItems.FindAsync(id);
            if (faq == null) return NotFound();
            faq.Question = item.Question; faq.Answer = item.Answer;
            faq.Category = item.Category; faq.SortOrder = item.SortOrder; faq.IsActive = item.IsActive;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var faq = await _context.FaqItems.FindAsync(id);
            if (faq == null) return NotFound();
            _context.FaqItems.Remove(faq);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
