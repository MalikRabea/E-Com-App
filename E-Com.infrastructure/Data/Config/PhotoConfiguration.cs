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
    internal class PhotoConfiguration : IEntityTypeConfiguration<Photo>
    {
        public void Configure(EntityTypeBuilder<Photo> builder)
        {
            builder.HasData(new Photo { Id = 1, ImageName = "test", ProductId = 1 });
        }
    }
}
