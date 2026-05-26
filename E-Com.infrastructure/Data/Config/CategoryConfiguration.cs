using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E_Com.Core.Entites.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_Com.infrastructure.Data.Config
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
           builder.Property( x => x.Name)
                .IsRequired()
                .HasMaxLength( 100);

           builder.Property(x => x.Id).IsRequired();

            builder.HasData(
                new Category { Id = 1, Name = "Electronics", Description = "Electronic devices and gadgets" }
              
            );




        }
    }
}
