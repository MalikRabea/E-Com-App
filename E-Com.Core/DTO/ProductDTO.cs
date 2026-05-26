using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E_Com.Core.Entites.Products;
using Microsoft.AspNetCore.Http;

namespace E_Com.Core.DTO
{
    public record ProductDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal NewPrice { get; set; }
        public decimal OldPrice { get; set; }

        public virtual List<PhotoDTO> Photos { get; set; }

        public string CategoryName { get; set; }

        public double rating { get; set; }

        //best seller
        public int SoldCount { get; set; }



    }
    public record ReturnProductDTO
    {

        public List<ProductDTO> products { get; set; }

        public int TotalCount { get; set; }
    }

        public record PhotoDTO
    {
        public string ImageName { get; set; }

        public int ProductId { get; set; }

    }
    public record AddProductDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal NewPrice { get; set; }
        public decimal OldPrice { get; set; }
        public int CategoryId { get; set; }

        public IFormFileCollection Photo { get; set; }
    }
    public record UpdateProductDTO: AddProductDTO
    {
        public int Id { get; set; }

    }
}
