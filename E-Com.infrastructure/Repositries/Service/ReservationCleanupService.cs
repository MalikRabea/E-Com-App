using E_Com.infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace E_Com.infrastructure.Repositries.Service
{
    // Marks expired stock reservations as released so the units return to the available pool
    public class ReservationCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReservationCleanupService> _logger;

        public ReservationCleanupService(IServiceScopeFactory scopeFactory, ILogger<ReservationCleanupService> logger)
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
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var now = DateTime.UtcNow;
                    var expired = await context.StockReservations
                        .Where(r => !r.Released && r.ExpiresAt <= now)
                        .ToListAsync(stoppingToken);

                    if (expired.Count > 0)
                    {
                        expired.ForEach(r => r.Released = true);
                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Released {Count} expired stock reservations", expired.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ReservationCleanupService error");
                }

                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }
    }
}
