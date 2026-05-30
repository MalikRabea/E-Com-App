using E_Com.API.Hubs;
using E_Com.Core.DTO;
using E_Com.Core.Entites;
using E_Com.Core.Entites.Order;
using E_Com.Core.Services;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
        private readonly IHubContext<OrderTrackingHub> _hub;
        private readonly IEmailService _emailService;

        public AdminController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            IHubContext<OrderTrackingHub> hub,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _hub = hub;
            _emailService = emailService;
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
                .Include(o => o.deliveryMethod).Include(o => o.orderItems)
                .OrderByDescending(o => o.OrderDate).Take(7).ToListAsync();

            var recentOrders = recentRaw.Select(o => new RecentOrderDTO
            {
                Id = o.Id, BuyerEmail = o.BuyerEmail,
                Total = o.SubTotal + (o.deliveryMethod != null ? o.deliveryMethod.Price : 0),
                Status = o.status.ToString(), OrderDate = o.OrderDate, ItemCount = o.orderItems?.Count ?? 0
            }).ToList();

            return Ok(new AdminStatsDTO
            {
                TotalProducts = totalProducts, TotalCategories = totalCategories,
                TotalOrders = totalOrders, PendingOrders = pendingOrders,
                TotalRevenue = totalRevenue, TotalUsers = totalUsers, RecentOrders = recentOrders
            });
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var total = await _context.Orders.CountAsync();
            var orders = await _context.Orders
                .Include(o => o.deliveryMethod).Include(o => o.orderItems)
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var result = orders.Select(o => new AdminOrderDTO
            {
                Id = o.Id, BuyerEmail = o.BuyerEmail, OrderDate = o.OrderDate,
                Total = o.SubTotal + (o.deliveryMethod != null ? o.deliveryMethod.Price : 0),
                Status = o.status.ToString(), ItemCount = o.orderItems?.Count ?? 0,
                DeliveryMethod = o.deliveryMethod?.Name ?? ""
            }).ToList();

            return Ok(new AdminOrderListDTO { Orders = result, Total = total, Page = page, PageSize = pageSize });
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

            // SignalR — notify the user watching their order
            await _hub.Clients.Group($"order-{id}")
                .SendAsync("OrderStatusUpdated", new { orderId = id, status = dto.Status });

            // Email notification (non-blocking)
            _ = SendStatusEmail(order.BuyerEmail, id, dto.Status);

            return Ok();
        }

        private async Task SendStatusEmail(string email, int orderId, string status)
        {
            try
            {
                var (icon, message) = status switch
                {
                    "Shipped"         => ("🚚", "Your order is on its way!"),
                    "Delivered"       => ("✅", "Your order has been delivered. Enjoy!"),
                    "PaymentReceived" => ("💳", "Your payment has been confirmed!"),
                    _                 => ("📦", $"Your order status was updated to: {status}")
                };

                var html = $@"<!DOCTYPE html><html><body style='font-family:Inter,sans-serif;max-width:600px;margin:0 auto;color:#1e293b'>
  <div style='background:#2563eb;padding:24px;border-radius:12px 12px 0 0;text-align:center'>
    <h1 style='color:#fff;margin:0'>{icon} Order Update</h1>
  </div>
  <div style='background:#f8fafc;padding:24px;border-radius:0 0 12px 12px;border:1px solid #e2e8f0'>
    <p>Hi <strong>{email}</strong>,</p>
    <p style='font-size:1.1rem'>{message}</p>
    <p>Order <strong>#{orderId}</strong> → <strong style='color:#2563eb'>{status}</strong></p>
  </div></body></html>";

                await _emailService.SendEmail(new EmailDTO
                {
                    To = email, Subject = $"Order #{orderId} — {status} | E-Shop", Content = html
                });
            }
            catch { /* silent — don't fail the request */ }
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
                    Id = user.Id, Email = user.Email ?? "",
                    DisplayName = user.DisplayName, Role = roles.FirstOrDefault() ?? "User"
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
                .Include(o => o.deliveryMethod).Include(o => o.orderItems)
                .OrderByDescending(o => o.OrderDate).ToListAsync();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Id,BuyerEmail,OrderDate,Total,Status,ItemCount,DeliveryMethod");
            foreach (var o in orders)
            {
                var total = o.SubTotal + (o.deliveryMethod != null ? o.deliveryMethod.Price : 0);
                sb.AppendLine(string.Join(",", o.Id, $"\"{o.BuyerEmail}\"",
                    o.OrderDate.ToString("yyyy-MM-dd HH:mm"), total.ToString("F2"),
                    o.status.ToString(), o.orderItems?.Count ?? 0, $"\"{o.deliveryMethod?.Name ?? ""}\""));
            }
            return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv",
                $"orders_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        [HttpGet("reviews")]
        public async Task<IActionResult> GetAllReviews()
        {
            var reviews = await _context.Ratings
                .Include(r => r.AppUser).Include(r => r.Product)
                .OrderByDescending(r => r.Review).ToListAsync();
            return Ok(reviews.Select(r => new AdminReviewDTO
            {
                Id = r.Id, ProductId = r.ProductId, ProductName = r.Product?.Name ?? "",
                UserName = r.AppUser?.DisplayName ?? r.AppUser?.Email ?? "",
                Stars = r.Stars, Content = r.content, ReviewTime = r.Review
            }));
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
                    Year = g.Key.Year, Month = g.Key.Month,
                    Revenue = g.Sum(o => o.SubTotal), Count = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month).ToListAsync();
            return Ok(data);
        }

        // ── New Analytics Endpoints ──

        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts()
        {
            var products = await _context.Products
                .Include(p => p.Photos)
                .OrderByDescending(p => p.SoldCount)
                .Take(5)
                .Select(p => new
                {
                    p.Id, p.Name, p.SoldCount, p.NewPrice,
                    Image = p.Photos.Select(ph => ph.ImageName).FirstOrDefault() ?? ""
                })
                .ToListAsync();
            return Ok(products);
        }

        [HttpGet("category-breakdown")]
        public async Task<IActionResult> GetCategoryBreakdown()
        {
            var items = await _context.OrderItems
                .Join(_context.Products, oi => oi.ProductItemId, p => p.Id, (oi, p) => new { oi, p.CategoryId })
                .Join(_context.Categories, x => x.CategoryId, c => c.Id, (x, c) => new
                {
                    Category = c.Name,
                    Revenue  = x.oi.Price * x.oi.Quantity
                })
                .ToListAsync();

            var data = items
                .GroupBy(x => x.Category)
                .Select(g => new { Category = g.Key, Revenue = g.Sum(x => x.Revenue), Count = g.Count() })
                .OrderByDescending(x => x.Revenue)
                .ToList();
            return Ok(data);
        }

        [HttpGet("daily-orders")]
        public async Task<IActionResult> GetDailyOrders()
        {
            var cutoff = DateTime.UtcNow.AddDays(-7).Date;
            var orders = await _context.Orders
                .Where(o => o.OrderDate >= cutoff)
                .ToListAsync();

            var data = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"), Count = g.Count(), Revenue = g.Sum(o => o.SubTotal) })
                .OrderBy(x => x.Date)
                .ToList();
            return Ok(data);
        }
    }
}
