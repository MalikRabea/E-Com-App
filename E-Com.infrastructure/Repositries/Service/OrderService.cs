using AutoMapper;
using E_Com.Core.DTO;
using E_Com.Core.Entites;
using E_Com.Core.Entites.Order;
using E_Com.Core.interfaces;
using E_Com.Core.Services;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E_Com.infrastructure.Repositries.Service
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPaymentService _paymentService;
        private readonly IEmailService _emailService;
        private readonly ILoyaltyService _loyaltyService;
        private readonly UserManager<AppUser> _userManager;

        public OrderService(
            IUnitOfWork unitOfWork,
            AppDbContext context,
            IMapper mapper,
            IPaymentService paymentService,
            IEmailService emailService,
            ILoyaltyService loyaltyService,
            UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _mapper = mapper;
            _paymentService = paymentService;
            _emailService = emailService;
            _loyaltyService = loyaltyService;
            _userManager = userManager;
        }

        public async Task<Orders> CreateOrdersAsync(OrderDTO orderDTO, string BuyerEmail)
        {
            var basket = await _unitOfWork.CustomerBasket.GetBasketAsync(orderDTO.basketId);

            var orderItems = new List<OrderItem>();
            foreach (var item in basket.basketItems)
            {
                var product = await _unitOfWork.ProductRepositry.GetByIdAsync(item.Id);
                if (product == null) throw new Exception($"Product {item.Id} not found");

                product.SoldCount += item.Quantity;
                await _unitOfWork.ProductRepositry.UpdateAsync(product);

                orderItems.Add(new OrderItem(product.Id, item.Image, product.Name, item.Price, item.Quantity));
            }

            var deliverMethod = await _context.DeliveryMethods
                .FirstOrDefaultAsync(m => m.Id == orderDTO.deliveryMethodId);

            var subTotal = orderItems.Sum(m => m.Price * m.Quantity);
            var ship = _mapper.Map<ShippingAddress>(orderDTO.shipAddress);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingOrder = await _context.Orders
                    .FirstOrDefaultAsync(m => m.PaymentIntentId == basket.PaymentIntentId);

                if (existingOrder is not null)
                {
                    _context.Orders.Remove(existingOrder);
                    await _paymentService.CreateOrUpdatePaymentAsync(basket.PaymentIntentId, deliverMethod.Id);
                }

                var order = new Orders(BuyerEmail, subTotal, ship, deliverMethod, orderItems, basket.PaymentIntentId)
                {
                    OrderDate = DateTime.UtcNow
                };

                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _unitOfWork.CustomerBasket.DeleteBasketAsync(orderDTO.basketId);

                // Send order confirmation email (non-blocking)
                _ = SendOrderConfirmationEmail(BuyerEmail, order, deliverMethod?.Price ?? 0);

                // Award loyalty points (1 point per $1)
                try
                {
                    var user = await _userManager.FindByEmailAsync(BuyerEmail);
                    if (user != null)
                    {
                        var points = _loyaltyService.CalculatePoints(subTotal);
                        await _loyaltyService.AwardPointsAsync(user.Id, points,
                            $"Order #{order.Id} reward", order.Id);
                    }
                }
                catch { /* loyalty failure should not block order */ }

                return order;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task SendOrderConfirmationEmail(string email, Orders order, decimal shipping)
        {
            try
            {
                var total = order.SubTotal + shipping;
                var itemRows = string.Join("", order.orderItems.Select(i =>
                    $"<tr><td style='padding:8px'>{i.ProductName}</td><td style='padding:8px'>x{i.Quantity}</td><td style='padding:8px'>${i.Price * i.Quantity:F2}</td></tr>"));

                var html = $@"
<!DOCTYPE html><html><body style='font-family:Inter,sans-serif;max-width:600px;margin:0 auto;color:#1e293b'>
  <div style='background:#2563eb;padding:24px;border-radius:12px 12px 0 0;text-align:center'>
    <h1 style='color:#fff;margin:0;font-size:1.5rem'>Order Confirmed! 🎉</h1>
  </div>
  <div style='background:#f8fafc;padding:24px;border-radius:0 0 12px 12px;border:1px solid #e2e8f0'>
    <p>Hi <strong>{email}</strong>,</p>
    <p>Thank you for your order! Here's your summary:</p>
    <div style='background:#fff;border-radius:8px;border:1px solid #e2e8f0;overflow:hidden;margin:16px 0'>
      <table style='width:100%;border-collapse:collapse'>
        <thead><tr style='background:#f1f5f9'>
          <th style='padding:10px 8px;text-align:left'>Product</th>
          <th style='padding:10px 8px'>Qty</th>
          <th style='padding:10px 8px'>Price</th>
        </tr></thead>
        <tbody>{itemRows}</tbody>
        <tfoot>
          <tr><td colspan='2' style='padding:10px 8px;font-weight:600'>Shipping</td><td style='padding:10px 8px'>${shipping:F2}</td></tr>
          <tr style='background:#f1f5f9'><td colspan='2' style='padding:10px 8px;font-weight:700;font-size:1.1rem'>Total</td><td style='padding:10px 8px;font-weight:700;color:#2563eb;font-size:1.1rem'>${total:F2}</td></tr>
        </tfoot>
      </table>
    </div>
    <p style='color:#64748b;font-size:0.85rem'>Order #{order.Id} · Placed on {order.OrderDate:MMMM dd, yyyy}</p>
    <p style='color:#64748b;font-size:0.85rem'>You earned <strong style='color:#2563eb'>{(int)Math.Floor(order.SubTotal)} loyalty points</strong> for this order! 🏆</p>
    <p>We'll notify you when your order ships.</p>
  </div>
</body></html>";

                await _emailService.SendEmail(new EmailDTO
                {
                    To      = email,
                    Subject = $"Order #{order.Id} Confirmed — E-Shop",
                    Content = html
                });
            }
            catch { /* email failure should not affect order */ }
        }

        public async Task<IReadOnlyList<OrderToReturnDTO>> GetAllOrdersForUserAsync(string BuyerEmail)
        {
            var orders = await _context.Orders
                .Where(o => o.BuyerEmail == BuyerEmail)
                .Include(o => o.orderItems)
                .Include(o => o.deliveryMethod)
                .ToListAsync();

            if (orders == null || !orders.Any()) return new List<OrderToReturnDTO>();

            var result = _mapper.Map<IReadOnlyList<OrderToReturnDTO>>(orders);

            foreach (var orderDto in result)
            {
                if (orderDto.orderItems != null)
                    foreach (var item in orderDto.orderItems)
                    {
                        item.ProductName ??= "";
                        item.MainImage   ??= "";
                    }
                else
                    orderDto.orderItems = new List<OrderItemDTO>();
            }

            return result.OrderByDescending(o => o.Id).ToList();
        }

        public async Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodAsync()
            => await _context.DeliveryMethods.AsNoTracking().ToListAsync();

        public async Task<OrderToReturnDTO> GetOrderByIdAsync(int Id, string BuyerEmail)
        {
            var order = await _context.Orders
                .Where(m => m.Id == Id && m.BuyerEmail == BuyerEmail)
                .Include(inc => inc.orderItems)
                .Include(inc => inc.deliveryMethod)
                .FirstOrDefaultAsync();
            return _mapper.Map<OrderToReturnDTO>(order);
        }
    }
}
