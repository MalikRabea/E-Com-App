using E_Com.API.Hubs;
using E_Com.Core.Entites.Order;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderTrackingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<OrderTrackingHub> _hub;

        public OrderTrackingController(AppDbContext context, IHubContext<OrderTrackingHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        // Public: get tracking timeline for an order (map waypoints)
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetTracking(int orderId)
        {
            var points = await _context.OrderTrackingPoints
                .Where(p => p.OrderId == orderId)
                .OrderBy(p => p.Timestamp)
                .Select(p => new { p.Status, p.Location, p.Latitude, p.Longitude, p.Note, p.Timestamp })
                .ToListAsync();

            return Ok(points);
        }

        // Admin: add a tracking checkpoint (and push to client live)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddPoint([FromBody] AddTrackingPointDTO dto)
        {
            var point = new OrderTrackingPoint
            {
                OrderId   = dto.OrderId,
                Status    = dto.Status,
                Location  = dto.Location,
                Latitude  = dto.Latitude,
                Longitude = dto.Longitude,
                Note      = dto.Note,
                Timestamp = DateTime.UtcNow
            };
            _context.OrderTrackingPoints.Add(point);
            await _context.SaveChangesAsync();

            await _hub.Clients.Group($"order-{dto.OrderId}")
                .SendAsync("TrackingUpdated", new { dto.OrderId, dto.Status, dto.Location, dto.Latitude, dto.Longitude });

            return Ok(point.Id);
        }
    }

    public class AddTrackingPointDTO
    {
        public int     OrderId   { get; set; }
        public string  Status    { get; set; }
        public string  Location  { get; set; }
        public double  Latitude  { get; set; }
        public double  Longitude { get; set; }
        public string? Note      { get; set; }
    }
}
