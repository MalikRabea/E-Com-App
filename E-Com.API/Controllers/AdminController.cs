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
    }
}
