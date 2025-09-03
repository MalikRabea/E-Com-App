using E_Com.Core.Entites.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Com.Core.Services
{
    public interface IProductService
    {
        Task<IReadOnlyList<Product>> GetBestSellersAsync(int count = 5);

    }
}
