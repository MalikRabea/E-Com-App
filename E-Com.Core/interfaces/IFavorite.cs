using E_Com.Core.Entites;
using E_Com.Core.Entites.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Com.Core.interfaces
{
    public interface IFavoriteRepository
    {
        Task AddAsync(Favorite favorite);
        Task RemoveAsync(Favorite favorite);
        Task<Favorite> GetByUserAndProductAsync(string userId, int productId);
        Task<IReadOnlyList<Product>> GetUserFavoritesAsync(string userId);
    }

}
