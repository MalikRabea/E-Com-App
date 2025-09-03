using E_Com.Core.Entites;
using E_Com.Core.Entites.Products;
using E_Com.Core.interfaces;
using E_Com.infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Com.infrastructure.Repositries
{
    public class FavoriteRepository : IFavoriteRepository
    {
        private readonly AppDbContext _context;
        private readonly IUnitOfWork _unitOfWork;

        public FavoriteRepository(AppDbContext context, IUnitOfWork unitOfWork)
        {
            _context = context;
            _unitOfWork = unitOfWork;
        }

        public async Task AddAsync(Favorite favorite)
        {
            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(Favorite favorite)
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
        }

        public async Task<Favorite> GetByUserAndProductAsync(string userId, int productId)
        {
            return await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
        }

        public async Task<IReadOnlyList<Product>> GetUserFavoritesAsync(string userId)
        {
            return await _context.Favorites
    .Where(f => f.UserId == userId)
    .Select(f => new Product
                {
                    Id = f.Product.Id,
                    Name = f.Product.Name,
                    Description = f.Product.Description,
                    NewPrice = f.Product.NewPrice,
                    OldPrice = f.Product.OldPrice,
                    SoldCount = f.Product.SoldCount,
                    Photos = f.Product.Photos.ToList()
                })
                .ToListAsync();
                    }

        //Task<IReadOnlyList<Product>> IFavoriteRepository.GetUserFavoritesAsync(string userId)
        //{
        //    throw new NotImplementedException();
        //}
    }

}
