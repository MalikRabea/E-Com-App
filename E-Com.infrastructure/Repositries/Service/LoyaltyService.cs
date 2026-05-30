using E_Com.Core.Entites.Loyalty;
using E_Com.Core.Services;
using E_Com.infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace E_Com.infrastructure.Repositries.Service
{
    public class LoyaltyService : ILoyaltyService
    {
        private readonly AppDbContext _context;

        public LoyaltyService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LoyaltyAccount> GetOrCreateAccountAsync(string userId)
        {
            var account = await _context.LoyaltyAccounts
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (account == null)
            {
                account = new LoyaltyAccount { UserId = userId, Points = 0, Tier = "Bronze" };
                _context.LoyaltyAccounts.Add(account);
                await _context.SaveChangesAsync();
            }
            return account;
        }

        public async Task AwardPointsAsync(string userId, int points, string description, int? orderId = null)
        {
            var account = await GetOrCreateAccountAsync(userId);
            account.Points += points;
            account.Tier = GetTier(account.Points);

            _context.PointsTransactions.Add(new PointsTransaction
            {
                LoyaltyAccountId = account.Id,
                Points      = points,
                Type        = PointsType.Earned,
                Description = description,
                OrderId     = orderId,
                CreatedAt   = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task<bool> RedeemPointsAsync(string userId, int points)
        {
            var account = await GetOrCreateAccountAsync(userId);
            if (account.Points < points) return false;

            account.Points -= points;
            account.Tier = GetTier(account.Points);

            _context.PointsTransactions.Add(new PointsTransaction
            {
                LoyaltyAccountId = account.Id,
                Points      = -points,
                Type        = PointsType.Redeemed,
                Description = $"Redeemed {points} points",
                CreatedAt   = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IReadOnlyList<PointsTransaction>> GetHistoryAsync(string userId)
        {
            var account = await _context.LoyaltyAccounts
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (account == null) return new List<PointsTransaction>();

            return await _context.PointsTransactions
                .Where(t => t.LoyaltyAccountId == account.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Take(30)
                .ToListAsync();
        }

        public int CalculatePoints(decimal orderTotal) => (int)Math.Floor(orderTotal);

        public string GetTier(int points) => points switch
        {
            >= 5000 => "Platinum",
            >= 2000 => "Gold",
            >= 500  => "Silver",
            _       => "Bronze"
        };
    }
}
