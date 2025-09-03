using E_Com.Core.DTO;
using E_Com.Core.Entites.Order;
using E_Com.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }
        [HttpPost("create-order")]
        public async Task<ActionResult> create(OrderDTO orderDTO)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            Orders order = await _orderService.CreateOrdersAsync(orderDTO, email);

            return Ok(order);
        }
        [Authorize]
        [HttpGet("get-orders-for-user")]
        public async Task<ActionResult<IReadOnlyList<OrderToReturnDTO>>> getorders()
        {
            var emailClaim = User.FindFirst(ClaimTypes.Email);
            if (emailClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var email = emailClaim.Value;
            var order = await _orderService.GetAllOrdersForUserAsync(email);

            if (order == null || !order.Any())
                return Ok(new List<OrderToReturnDTO>()); // بدل 500 إذا لا يوجد طلبات

            return Ok(order);
        }



        [HttpGet("get-order-by-id/{id}")]
        public async Task<ActionResult<OrderToReturnDTO>> getOrderById(int id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var order = await _orderService.GetOrderByIdAsync(id, email);
            return Ok(order);
        }


        [HttpGet("get-delivery")]
        public async Task<ActionResult> GetDeliver()
        => Ok(await _orderService.GetDeliveryMethodAsync());

    }
}
