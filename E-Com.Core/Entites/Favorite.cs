using E_Com.Core.Entites.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Com.Core.Entites
{
    public class Favorite
    {
        public int Id { get; set; }

        // الربط مع المستخدم
        public string UserId { get; set; } = string.Empty;
        public AppUser? User { get; set; }

        // الربط مع المنتج
        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
