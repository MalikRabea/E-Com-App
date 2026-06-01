using E_Com.Core.Entites;
using E_Com.Core.Entites.Order;
using E_Com.Core.interfaces;
using E_Com.Core.Services;
using E_Com.infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace E_Com.infrastructure.Repositries.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _work;
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public PaymentService(IUnitOfWork work, IConfiguration configuration, AppDbContext context)
        {
            _work    = work;
            _config  = configuration;
            _context = context;
        }

        public async Task<CustomerBasket> CreateOrUpdatePaymentAsync(string basketId, int? delivertMethodId)
        {
            CustomerBasket basket = await _work.CustomerBasket.GetBasketAsync(basketId);
            StripeConfiguration.ApiKey = _config["StripSetting:secretKey"];

            decimal shippingPrice = 0m;
            if (delivertMethodId.HasValue)
            {
                var delivery = await _context.DeliveryMethods.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == delivertMethodId.Value);
                shippingPrice = delivery?.Price ?? 0;
            }

            foreach (var item in basket.basketItems)
            {
                var product = await _work.ProductRepositry.GetByIdAsync(item.Id);
                item.Price = product.NewPrice;
            }

            var paymentIntentService = new PaymentIntentService();
            PaymentIntent intent;

            if (string.IsNullOrEmpty(basket.PaymentIntentId))
            {
                var opts = new PaymentIntentCreateOptions
                {
                    Amount   = (long)(basket.basketItems.Sum(m => m.Quantity * m.Price * 100)) + (long)(shippingPrice * 100),
                    Currency = "USD",
                    PaymentMethodTypes = new List<string> { "card" }
                };
                intent = await paymentIntentService.CreateAsync(opts);
                basket.PaymentIntentId = intent.Id;
                basket.ClientSecret    = intent.ClientSecret;
            }
            else
            {
                var opts = new PaymentIntentUpdateOptions
                {
                    Amount = (long)(basket.basketItems.Sum(m => m.Quantity * m.Price * 100)) + (long)(shippingPrice * 100)
                };
                await paymentIntentService.UpdateAsync(basket.PaymentIntentId, opts);
            }

            await _work.CustomerBasket.UpdateBasketAsync(basket);

            // Track for abandoned cart detection
            if (!string.IsNullOrEmpty(basket.PaymentIntentId))
                await TrackAbandonedCart(basket);

            return basket;
        }

        private async Task TrackAbandonedCart(CustomerBasket basket)
        {
            try
            {
                // We can only track if the basket has an associated email
                // The basket doesn't store email directly, but we can track by basket ID
                var existing = await _context.AbandonedCartTrackers
                    .FirstOrDefaultAsync(t => t.BasketId == basket.Id.ToString());

                if (existing == null)
                {
                    _context.AbandonedCartTrackers.Add(new AbandonedCartTracker
                    {
                        BasketId        = basket.Id.ToString(),
                        PaymentIntentId = basket.PaymentIntentId,
                        UserEmail       = "", // Will be set by order creation if user is logged in
                        CreatedAt       = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }
                else
                {
                    existing.PaymentIntentId = basket.PaymentIntentId;
                    await _context.SaveChangesAsync();
                }
            }
            catch { /* non-critical */ }
        }

        public async Task<Orders> UpdateOrderFaild(string paymentIntentId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(m => m.PaymentIntentId == paymentIntentId);
            if (order is null) return null;
            order.status = Status.PaymentFailed;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Orders> UpdateOrderSuccess(string paymentIntentId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(m => m.PaymentIntentId == paymentIntentId);
            if (order is null) return null;
            order.status = Status.PaymentReceived;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return order;
        }
    }
}
