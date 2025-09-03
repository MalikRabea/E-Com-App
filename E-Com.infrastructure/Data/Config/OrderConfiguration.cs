using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E_Com.Core.Entites.Order;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_Com.infrastructure.Data.Config
{
    public class OrderConfiguration : IEntityTypeConfiguration<Orders>
    {
        public void Configure(EntityTypeBuilder<Orders> builder)
        {
            builder.OwnsOne(x => x.shippingAddress,
                sa =>  { sa.WithOwner();   });

            builder.HasMany(x => x.orderItems)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.status).HasConversion(o => o.ToString(),
                o => (Status)Enum.Parse(typeof(Status), o));

            builder.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");


        }
    }
}
