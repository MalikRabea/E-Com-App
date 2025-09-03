using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E_Com.Core.DTO;
using E_Com.Core.Entites.Products;
using E_Com.Core.Sharing;

namespace E_Com.Core.interfaces
{
    public interface IProductRepositry : IGenericRepositry<Product>
    {
        //for futuer
        Task<ReturnProductDTO> GetAllAsync(ProductParams productParams);

        Task<IReadOnlyList<Product>> GetBestSellersAsync(int count);

        Task<bool> AddAsync(AddProductDTO productDTO);
        Task<bool> UpdateAsync(UpdateProductDTO updateProductDTO );
        Task DeleteAsync(Product product );



    }
}
