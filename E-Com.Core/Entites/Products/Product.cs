using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Com.Core.Entites.Products
{
    public class Product : BaseEntity<int>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal NewPrice { get; set; }
        public decimal OldPrice { get; set; }


        public virtual List<Photo> Photos { get; set; } 
        public int CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))]
        public virtual Category Category { get; set; }
        // Additional properties can be added as needed

        public double rating { get; set; }




        //best seller   
        public int SoldCount { get; set; } = 0;



    }
}
