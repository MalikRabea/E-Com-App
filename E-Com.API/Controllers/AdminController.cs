using E_Com.Core.DTO;
using E_Com.Core.Entites;
using E_Com.Core.Entites.Order;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public AdminController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalProducts   = await _context.Products.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();
            var totalOrders     = await _context.Orders.CountAsync();
            var pendingOrders   = await _context.Orders.CountAsync(o => o.status == Status.Pending);
            var totalUsers      = await _userManager.Users.CountAsync();

            var paidOrders = await _context.Orders
                .Where(o => o.status == Status.PaymentReceived)
                .Include(o => o.deliveryMethod)
                .ToListAsync();

            var totalRevenue = paidOrders.Sum(o =>
                o.SubTotal + (o.deliveryMethod != null ? o.deliveryMethod.Price : 0));

            var recentRaw = await _context.Orders
                .Include(o => o.deliveryMethod)
                .Include(o => o.orderItems)
                .OrderByDescending(o => o.OrderDate)
                .Take(7)
                .ToListAsync();

            var recentOrders = recentRaw.Select(o => new RecentOrderDTO
            {
                Id         = o.Id,
                BuyerEmail = o.BuyerEmail,
                Total      = o.SubTotal + (o.deliveryMethod != null ? o.deliveryMethod.Price : 0),
                Status     = o.status.ToString(),
                OrderDate  = o.OrderDate,
                ItemCount  = o.orderItems?.Count ?? 0
            }).ToList();

            return Ok(new AdminStatsDTO
            {
                TotalProducts   = totalProducts,
                TotalCategories = totalCategories,
                TotalOrders     = totalOrders,
                PendingOrders   = pendingOrders,
                TotalRevenue    = totalRevenue,
                TotalUsers      = totalUsers,
                RecentOrders    = recentOrders
            });
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var total = await _context.Orders.CountAsync();

            var orders = await _context.Orders
                .Include(o => o.deliveryMethod)
                .Include(o => o.orderItems)
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = orders.Select(o => new AdminOrderDTO
            {
                Id             = o.Id,
                BuyerEmail     = o.BuyerEmail,
                OrderDate      = o.OrderDate,
                Total          = o.SubTotal + (o.deliveryMethod != null ? o.deliveryMethod.Price : 0),
                Status         = o.status.ToString(),
                ItemCount      = o.orderItems?.Count ?? 0,
                DeliveryMethod = o.deliveryMethod?.Name ?? ""
            }).ToList();

            return Ok(new AdminOrderListDTO
            {
                Orders   = result,
                Total    = total,
                Page     = page,
                PageSize = pageSize
            });
        }

        [HttpPatch("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateStatusDTO dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (!Enum.TryParse<Status>(dto.Status, out var newStatus))
                return BadRequest("Invalid status value");
            order.status = newStatus;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var result = new List<UserDTO>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new UserDTO
                {
                    Id          = user.Id,
                    Email       = user.Email ?? "",
                    DisplayName = user.DisplayName,
                    Role        = roles.FirstOrDefault() ?? "User"
                });
            }
            return Ok(result);
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded) return BadRequest("Could not delete user");
            return Ok();
        }

        [HttpGet("orders/export")]
        public async Task<IActionResult> ExportOrdersCsv()
        {
            var orders = await _context.Orders
                .Include(o => o.deliveryMethod)
                .Include(o => o.orderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Id,BuyerEmail,OrderDate,Total,Status,ItemCount,DeliveryMethod");
            foreach (var o in orders)
            {
                var total = o.SubTotal + (o.deliveryMethod != null ? o.deliveryMethod.Price : 0);
                var row = string.Join(",",
                    o.Id,
                    $"\"{o.BuyerEmail}\"",
                    o.OrderDate.ToString("yyyy-MM-dd HH:mm"),
                    total.ToString("F2"),
                    o.status.ToString(),
                    o.orderItems?.Count ?? 0,
                    $"\"{o.deliveryMethod?.Name ?? ""}\"");
                sb.AppendLine(row);
            }
            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"orders_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        [HttpGet("reviews")]
        public async Task<IActionResult> GetAllReviews()
        {
            var reviews = await _context.Ratings
                .Include(r => r.AppUser)
                .Include(r => r.Product)
                .OrderByDescending(r => r.Review)
                .ToListAsync();

            var result = reviews.Select(r => new AdminReviewDTO
            {
                Id          = r.Id,
                ProductId   = r.ProductId,
                ProductName = r.Product?.Name ?? "",
                UserName    = r.AppUser?.DisplayName ?? r.AppUser?.Email ?? "",
                Stars       = r.Stars,
                Content     = r.content,
                ReviewTime  = r.Review
            }).ToList();
            return Ok(result);
        }

        [HttpDelete("reviews/{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Ratings.FindAsync(id);
            if (review == null) return NotFound();
            _context.Ratings.Remove(review);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("monthly-sales")]
        public async Task<IActionResult> GetMonthlySales()
        {
            var cutoff = DateTime.UtcNow.AddMonths(-6);
            var data = await _context.Orders
                .Where(o => o.status == Status.PaymentReceived && o.OrderDate >= cutoff)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new MonthlySalesDTO
                {
                    Year    = g.Key.Year,
                    Month   = g.Key.Month,
                    Revenue = g.Sum(o => o.SubTotal),
                    Count   = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();
            return Ok(data);
        }
    }
}
