using System.Reflection;
using E_Com.Core.Entites;
using E_Com.Core.Entites.Loyalty;
using E_Com.Core.Entites.Order;
using E_Com.Core.Entites.Products;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace E_Com.infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public virtual DbSet<Category>          Categories          { get; set; }
        public virtual DbSet<Product>           Products            { get; set; }
        public virtual DbSet<Photo>             Photos              { get; set; }
        public virtual DbSet<Address>           Addresses           { get; set; }
        public virtual DbSet<Orders>            Orders              { get; set; }
        public virtual DbSet<OrderItem>         OrderItems          { get; set; }
        public virtual DbSet<Rating>            Ratings             { get; set; }
        public virtual DbSet<DeliveryMethod>    DeliveryMethods     { get; set; }
        public virtual DbSet<Favorite>          Favorites           { get; set; }
        public virtual DbSet<Coupon>            Coupons             { get; set; }

        // Loyalty
        public virtual DbSet<LoyaltyAccount>    LoyaltyAccounts     { get; set; }
        public virtual DbSet<PointsTransaction> PointsTransactions  { get; set; }

        // Variants
        public virtual DbSet<ProductVariant>    ProductVariants     { get; set; }
        public virtual DbSet<VariantOption>     VariantOptions      { get; set; }

        // Returns
        public virtual DbSet<ReturnRequest>       ReturnRequests       { get; set; }

        // Abandoned Cart
        public virtual DbSet<AbandonedCartTracker> AbandonedCartTrackers { get; set; }

        // Push Notifications
        public virtual DbSet<PushSubscription>  PushSubscriptions   { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.User).WithMany().HasForeignKey(f => f.UserId);
            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.Product).WithMany().HasForeignKey(f => f.ProductId);

            modelBuilder.Entity<LoyaltyAccount>()
                .HasMany(a => a.Transactions)
                .WithOne(t => t.LoyaltyAccount)
                .HasForeignKey(t => t.LoyaltyAccountId);

            modelBuilder.Entity<ProductVariant>()
                .HasMany(v => v.Options)
                .WithOne(o => o.Variant)
                .HasForeignKey(o => o.VariantId);
        }
    }
}
