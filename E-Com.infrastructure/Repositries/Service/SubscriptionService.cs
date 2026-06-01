using E_Com.Core.DTO;
using E_Com.Core.Services;
using E_Com.infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace E_Com.infrastructure.Repositries.Service
{
    // Processes due subscriptions: emails the customer that their recurring order is ready,
    // and rolls the NextDeliveryDate forward.
    public class SubscriptionService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(IServiceScopeFactory scopeFactory, ILogger<SubscriptionService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await ProcessDue(); }
                catch (Exception ex) { _logger.LogError(ex, "SubscriptionService error"); }
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }

        private async Task ProcessDue()
        {
            using var scope = _scopeFactory.CreateScope();
            var context      = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.UtcNow;
            var due = await context.Subscriptions
                .Where(s => s.IsActive && s.NextDeliveryDate <= now)
                .Include(s => s.Product)
                .ToListAsync();

            foreach (var sub in due)
            {
                var unit       = sub.Product?.NewPrice ?? 0;
                var discounted = unit * (1 - sub.DiscountPercent / 100m);
                var lineTotal  = discounted * sub.Quantity;

                var html = $@"<!DOCTYPE html><html><body style='font-family:Inter,sans-serif;max-width:600px;margin:0 auto;color:#1e293b'>
  <div style='background:#10b981;padding:24px;border-radius:12px 12px 0 0;text-align:center'>
    <h1 style='color:#fff;margin:0'>🔁 Your Subscription is Ready</h1>
  </div>
  <div style='background:#f8fafc;padding:24px;border-radius:0 0 12px 12px;border:1px solid #e2e8f0'>
    <p>Hi <strong>{sub.UserEmail}</strong>,</p>
    <p>Your {sub.Interval.ToLower()} subscription for <strong>{sub.Product?.Name}</strong> (x{sub.Quantity}) is due!</p>
    <p>Subscriber price: <strong style='color:#10b981'>${lineTotal:F2}</strong> ({sub.DiscountPercent}% off)</p>
    <a href='https://e-com-app-ngx.onrender.com/shop/product-details/{sub.ProductId}'
       style='display:inline-block;background:#10b981;color:#fff;padding:12px 28px;border-radius:8px;text-decoration:none;font-weight:700;margin-top:8px'>
      Confirm Order →
    </a>
  </div></body></html>";

                try
                {
                    await emailService.SendEmail(new EmailDTO
                    {
                        To      = sub.UserEmail,
                        Subject = $"Your {sub.Interval} subscription is ready — E-Shop",
                        Content = html
                    });
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Subscription email failed for {Email}", sub.UserEmail); }

                sub.LastProcessed    = now;
                sub.NextDeliveryDate = SubscriptionsCalc(sub.Interval, now);
            }

            await context.SaveChangesAsync();
        }

        private static DateTime SubscriptionsCalc(string interval, DateTime from) => interval switch
        {
            "Weekly"    => from.AddDays(7),
            "Quarterly" => from.AddMonths(3),
            _           => from.AddMonths(1),
        };
    }
}
