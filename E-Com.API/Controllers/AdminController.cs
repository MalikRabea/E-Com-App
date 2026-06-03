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
        private readonly INotificationService _notifications;

        public AdminController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            IHubContext<OrderTrackingHub> hub,
            IEmailService emailService,
            INotificationService notifications)
        {
            _context = context;
            _userManager = userManager;
            _hub = hub;
            _emailService = emailService;
            _notifications = notifications;
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

            // In-app notification to the buyer
            var (nIcon, nMsg) = dto.Status switch
            {
                "Shipped"         => ("local_shipping", "Your order is on its way! 🚚"),
                "Delivered"       => ("done_all",       "Your order has been delivered. Enjoy! ✅"),
                "PaymentReceived" => ("payments",       "Payment confirmed for your order."),
                _                 => ("info",           $"Your order status is now: {dto.Status}")
            };
            await _notifications.NotifyByEmailAsync(order.BuyerEmail, "order", nIcon,
                $"Order #{id} — {dto.Status}", nMsg, $"/orders?id={id}");

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
            var orders = await _context.Orders.Where(o => o.OrderDate >= cutoff).ToListAsync();
            var data   = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"), Count = g.Count(), Revenue = g.Sum(o => o.SubTotal) })
                .OrderBy(x => x.Date).ToList();
            return Ok(data);
        }

        // ── Return Requests ──

        [HttpGet("returns")]
        public async Task<IActionResult> GetReturnRequests([FromQuery] string? status = null)
        {
            var query = _context.ReturnRequests.AsQueryable();
            if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);

            var data = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new { r.Id, r.OrderId, r.UserEmail, r.Reason, r.Description, r.Status, r.AdminNote, r.CreatedAt, r.UpdatedAt })
                .ToListAsync();
            return Ok(data);
        }

        [HttpPatch("returns/{id}")]
        public async Task<IActionResult> UpdateReturnStatus(int id, [FromBody] UpdateReturnDTO dto)
        {
            var request = await _context.ReturnRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status    = dto.Status;
            request.AdminNote = dto.AdminNote ?? "";
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // In-app notification to the user
            var approved = dto.Status == "Approved";
            await _notifications.NotifyByEmailAsync(request.UserEmail,
                approved ? "success" : "warning",
                approved ? "check_circle" : "cancel",
                $"Return #{request.Id} {dto.Status}",
                approved ? "Your return was approved. Refund is being processed."
                         : $"Your return was rejected. {request.AdminNote}",
                $"/orders?id={request.OrderId}");

            // Email user about decision (non-blocking)
            _ = SendReturnDecisionEmail(request);

            return Ok();
        }

        private async Task SendReturnDecisionEmail(ReturnRequest r)
        {
            try
            {
                var (icon, msg) = r.Status == "Approved"
                    ? ("✅", "Your return request has been approved. A refund will be processed within 3-5 business days.")
                    : ("❌", $"Your return request has been rejected. {r.AdminNote}");

                var html = $@"<!DOCTYPE html><html><body style='font-family:Inter,sans-serif;max-width:600px;margin:0 auto;color:#1e293b'>
  <div style='background:#2563eb;padding:24px;border-radius:12px 12px 0 0;text-align:center'>
    <h1 style='color:#fff;margin:0'>{icon} Return Request Update</h1>
  </div>
  <div style='background:#f8fafc;padding:24px;border-radius:0 0 12px 12px;border:1px solid #e2e8f0'>
    <p>Hi <strong>{r.UserEmail}</strong>,</p>
    <p>{msg}</p>
    <p>Order #{r.OrderId} · Reason: {r.Reason}</p>
  </div></body></html>";

                await _emailService.SendEmail(new E_Com.Core.DTO.EmailDTO
                {
                    To      = r.UserEmail,
                    Subject = $"Return Request #{r.Id} — {r.Status} | E-Shop",
                    Content = html
                });
            }
            catch { }
        }

        // ── Abandoned Carts ──

        [HttpGet("abandoned-carts")]
        public async Task<IActionResult> GetAbandonedCarts()
        {
            var cutoff = DateTime.UtcNow.AddHours(-24);
            var carts = await _context.AbandonedCartTrackers
                .Where(t => !string.IsNullOrEmpty(t.UserEmail) && t.CreatedAt <= cutoff)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new { t.Id, t.UserEmail, t.BasketId, t.CreatedAt, t.EmailSent, t.EmailSentAt })
                .ToListAsync();
            return Ok(carts);
        }

        // ── Customer Segmentation ──

        [HttpGet("segments")]
        public async Task<IActionResult> GetSegments()
        {
            var paidOrders = await _context.Orders
                .Where(o => o.status == Status.PaymentReceived)
                .Select(o => new { o.BuyerEmail, o.SubTotal, o.OrderDate })
                .ToListAsync();

            var byUser = paidOrders.GroupBy(o => o.BuyerEmail)
                .Select(g => new { Email = g.Key, Spent = g.Sum(x => x.SubTotal), Last = g.Max(x => x.OrderDate), Count = g.Count() })
                .ToList();

            var now = DateTime.UtcNow;
            var vip      = byUser.Count(u => u.Spent >= 500);
            var inactive = byUser.Count(u => (now - u.Last).TotalDays > 60);
            var totalUsers = await _userManager.Users.CountAsync();
            var buyers   = byUser.Select(u => u.Email).ToHashSet();
            var newUsers = totalUsers - buyers.Count; // never purchased

            return Ok(new
            {
                All      = totalUsers,
                VIP      = vip,
                Inactive = inactive,
                New      = newUsers
            });
        }

        [HttpPost("campaigns/send")]
        public async Task<IActionResult> SendCampaign([FromBody] SendCampaignDTO dto)
        {
            var recipients = await ResolveSegmentEmails(dto.Segment);
            var sender = await _userManager.GetUserAsync(User);

            // record campaign
            _context.EmailCampaigns.Add(new E_Com.Core.Entites.Marketing.EmailCampaign
            {
                Subject = dto.Subject, Body = dto.Body, Segment = dto.Segment,
                Recipients = recipients.Count, SentByUserId = sender?.Id ?? ""
            });
            await _context.SaveChangesAsync();

            // fire-and-forget emails
            _ = Task.Run(async () =>
            {
                foreach (var email in recipients)
                {
                    try
                    {
                        var html = $@"<!DOCTYPE html><html><body style='font-family:Inter,sans-serif;max-width:600px;margin:0 auto;color:#1e293b'>
  <div style='background:#2563eb;padding:24px;border-radius:12px 12px 0 0;text-align:center'>
    <h1 style='color:#fff;margin:0'>E-Shop</h1></div>
  <div style='background:#f8fafc;padding:24px;border-radius:0 0 12px 12px;border:1px solid #e2e8f0'>{dto.Body}</div>
</body></html>";
                        await _emailService.SendEmail(new E_Com.Core.DTO.EmailDTO { To = email, Subject = dto.Subject, Content = html });
                    }
                    catch { }
                }
            });

            return Ok(new { sent = recipients.Count });
        }

        [HttpGet("campaigns")]
        public async Task<IActionResult> GetCampaigns()
            => Ok(await _context.EmailCampaigns.OrderByDescending(c => c.SentAt)
                .Select(c => new { c.Id, c.Subject, c.Segment, c.Recipients, c.SentAt }).ToListAsync());

        private async Task<List<string>> ResolveSegmentEmails(string segment)
        {
            var paidOrders = await _context.Orders
                .Where(o => o.status == Status.PaymentReceived)
                .Select(o => new { o.BuyerEmail, o.SubTotal, o.OrderDate })
                .ToListAsync();

            var byUser = paidOrders.GroupBy(o => o.BuyerEmail)
                .Select(g => new { Email = g.Key, Spent = g.Sum(x => x.SubTotal), Last = g.Max(x => x.OrderDate) })
                .ToList();

            var now = DateTime.UtcNow;
            switch (segment)
            {
                case "VIP":      return byUser.Where(u => u.Spent >= 500).Select(u => u.Email).ToList();
                case "Inactive": return byUser.Where(u => (now - u.Last).TotalDays > 60).Select(u => u.Email).ToList();
                case "New":
                    var buyers = byUser.Select(u => u.Email).ToHashSet();
                    return (await _userManager.Users.Where(u => u.Email != null).Select(u => u.Email!).ToListAsync())
                        .Where(e => !buyers.Contains(e)).ToList();
                default:
                    return await _userManager.Users.Where(u => u.Email != null).Select(u => u.Email!).ToListAsync();
            }
        }

        // ── Inventory Movements ──

        [HttpGet("inventory/movements")]
        public async Task<IActionResult> GetMovements([FromQuery] int? productId = null)
        {
            var query = _context.InventoryMovements.AsQueryable();
            if (productId.HasValue) query = query.Where(m => m.ProductId == productId);
            var data = await query.OrderByDescending(m => m.CreatedAt).Take(100)
                .Select(m => new { m.Id, m.ProductId, m.ProductName, m.Change, m.NewStock, m.Reason, m.PerformedBy, m.CreatedAt })
                .ToListAsync();
            return Ok(data);
        }

        [HttpGet("inventory/reorder-alerts")]
        public async Task<IActionResult> ReorderAlerts([FromQuery] int threshold = 5)
        {
            var low = await _context.Products
                .Where(p => p.StockQuantity <= threshold)
                .OrderBy(p => p.StockQuantity)
                .Select(p => new { p.Id, p.Name, p.StockQuantity, p.SoldCount })
                .ToListAsync();
            return Ok(low);
        }

        [HttpPost("inventory/restock")]
        public async Task<IActionResult> Restock([FromBody] RestockDTO dto)
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null) return NotFound();

            product.StockQuantity += dto.Amount;
            _context.InventoryMovements.Add(new E_Com.Core.Entites.Inventory.InventoryMovement
            {
                ProductId = product.Id, ProductName = product.Name,
                Change = dto.Amount, NewStock = product.StockQuantity,
                Reason = dto.Reason ?? "Restock", PerformedBy = User.Identity?.Name
            });
            await _context.SaveChangesAsync();
            return Ok(new { product.StockQuantity });
        }
    }

    public class UpdateReturnDTO
    {
        public string  Status    { get; set; }
        public string? AdminNote { get; set; }
    }

    public class SendCampaignDTO
    {
        public string Subject { get; set; }
        public string Body    { get; set; }
        public string Segment { get; set; }
    }

    public class RestockDTO
    {
        public int     ProductId { get; set; }
        public int     Amount    { get; set; }
        public string? Reason    { get; set; }
    }
}
