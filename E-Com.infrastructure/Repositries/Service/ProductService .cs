using E_Com.Core.Entites.Products;
using E_Com.Core.interfaces;
using E_Com.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace E_Com.infrastructure.Repositries.Service
{
    public class ProductService : IProductService
    {
        private readonly IProductRepositry _productRepository;

        public ProductService(IProductRepositry productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IReadOnlyList<Product>> GetBestSellersAsync(int count = 8)
        {
            return await _productRepository.GetBestSellersAsync(count);
        }
    }
}
