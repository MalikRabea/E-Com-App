using E_Com.Core.Entites.Loyalty;

namespace E_Com.Core.Services
{
    public interface ILoyaltyService
    {
        Task<LoyaltyAccount> GetOrCreateAccountAsync(string userId);
        Task AwardPointsAsync(string userId, int points, string description, int? orderId = null);
        Task<bool> RedeemPointsAsync(string userId, int points);
        Task<IReadOnlyList<PointsTransaction>> GetHistoryAsync(string userId);
        int CalculatePoints(decimal orderTotal);
        string GetTier(int points);
    }
}
