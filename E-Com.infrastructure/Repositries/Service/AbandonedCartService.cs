using E_Com.Core.DTO;
using E_Com.Core.Entites;
using E_Com.Core.Services;
using E_Com.infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace E_Com.infrastructure.Repositries.Service
{
    public class AbandonedCartService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AbandonedCartService> _logger;

        public AbandonedCartService(IServiceScopeFactory scopeFactory, ILogger<AbandonedCartService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAbandonedCarts();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AbandonedCartService");
                }
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task ProcessAbandonedCarts()
        {
            using var scope = _scopeFactory.CreateScope();
            var context      = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var cutoff = DateTime.UtcNow.AddHours(-24);

            var abandoned = await context.AbandonedCartTrackers
                .Where(t => !t.EmailSent && t.CreatedAt <= cutoff)
                .ToListAsync();

            foreach (var tracker in abandoned)
            {
                // Skip if an order was placed with this payment intent
                if (!string.IsNullOrEmpty(tracker.PaymentIntentId))
                {
                    var orderExists = await context.Orders
                        .AnyAsync(o => o.PaymentIntentId == tracker.PaymentIntentId
                                    && o.status != E_Com.Core.Entites.Order.Status.Pending);
                    if (orderExists) { tracker.EmailSent = true; continue; }
                }

                var html = $@"<!DOCTYPE html><html><body style='font-family:Inter,sans-serif;max-width:600px;margin:0 auto;color:#1e293b'>
  <div style='background:#2563eb;padding:24px;border-radius:12px 12px 0 0;text-align:center'>
    <h1 style='color:#fff;margin:0'>🛒 You left something behind!</h1>
  </div>
  <div style='background:#f8fafc;padding:24px;border-radius:0 0 12px 12px;border:1px solid #e2e8f0'>
    <p>Hi <strong>{tracker.UserEmail}</strong>,</p>
    <p>You have items waiting in your cart. Complete your purchase before they sell out!</p>
    <a href='https://e-com-app-ngx.onrender.com/basket'
       style='display:inline-block;background:#2563eb;color:#fff;padding:12px 28px;border-radius:8px;text-decoration:none;font-weight:700;margin:16px 0'>
      Return to Cart →
    </a>
    <p style='color:#64748b;font-size:0.85rem'>Use code <strong>COMEBACK10</strong> for 10% off your order!</p>
  </div>
</body></html>";

                try
                {
                    await emailService.SendEmail(new EmailDTO
                    {
                        To      = tracker.UserEmail,
                        Subject = "You left items in your cart — E-Shop",
                        Content = html
                    });
                    tracker.EmailSent   = true;
                    tracker.EmailSentAt = DateTime.UtcNow;
                    _logger.LogInformation("Abandoned cart email sent to {Email}", tracker.UserEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send abandoned cart email to {Email}", tracker.UserEmail);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
