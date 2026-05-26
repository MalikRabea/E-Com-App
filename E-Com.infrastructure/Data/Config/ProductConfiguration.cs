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
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.Property(x => x.Name)
                 .IsRequired();

            builder.Property(x => x.Description).IsRequired();

            builder.Property(x => x.NewPrice)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
            builder.Property(x => x.OldPrice)
               .IsRequired()
               .HasColumnType("decimal(18,2)");

            builder.HasData(
                new Product { Id = 1, Name = "Smartphone", Description = "Latest ", NewPrice = 1, CategoryId = 1 }
               
               
            );

        }
    }
}
