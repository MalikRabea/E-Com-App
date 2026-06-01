using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace E_Com.infrastructure.Data.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(E_Com.infrastructure.Data.AppDbContext))]
    [Migration("20260602000001_AddCommercialFeatures")]
    public partial class AddCommercialFeatures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GiftCards
            migrationBuilder.CreateTable(
                name: "GiftCards",
                columns: table => new
                {
                    Id                = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code              = table.Column<string>(nullable: false),
                    InitialBalance    = table.Column<decimal>(nullable: false),
                    CurrentBalance    = table.Column<decimal>(nullable: false),
                    IssuedToEmail     = table.Column<string>(nullable: true),
                    PurchasedByUserId = table.Column<string>(nullable: true),
                    Message           = table.Column<string>(nullable: true),
                    IsActive          = table.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt         = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    ExpiryDate        = table.Column<DateTime>(nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_GiftCards", x => x.Id));

            migrationBuilder.CreateIndex("IX_GiftCards_Code", "GiftCards", "Code", unique: true);

            // Subscriptions
            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id               = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId           = table.Column<string>(nullable: false),
                    UserEmail        = table.Column<string>(nullable: false),
                    ProductId        = table.Column<int>(nullable: false),
                    Quantity         = table.Column<int>(nullable: false, defaultValue: 1),
                    Interval         = table.Column<string>(nullable: false, defaultValue: "Monthly"),
                    DiscountPercent  = table.Column<decimal>(nullable: false, defaultValue: 10m),
                    NextDeliveryDate = table.Column<DateTime>(nullable: false),
                    IsActive         = table.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt        = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    LastProcessed    = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey("FK_Subscriptions_Products",
                        x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_Subscriptions_UserId",    "Subscriptions", "UserId");
            migrationBuilder.CreateIndex("IX_Subscriptions_ProductId", "Subscriptions", "ProductId");

            // OrderTrackingPoints
            migrationBuilder.CreateTable(
                name: "OrderTrackingPoints",
                columns: table => new
                {
                    Id        = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId   = table.Column<int>(nullable: false),
                    Status    = table.Column<string>(nullable: false),
                    Location  = table.Column<string>(nullable: false),
                    Latitude  = table.Column<double>(nullable: false),
                    Longitude = table.Column<double>(nullable: false),
                    Note      = table.Column<string>(nullable: true),
                    Timestamp = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table => table.PrimaryKey("PK_OrderTrackingPoints", x => x.Id));

            migrationBuilder.CreateIndex("IX_OrderTrackingPoints_OrderId", "OrderTrackingPoints", "OrderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("OrderTrackingPoints");
            migrationBuilder.DropTable("Subscriptions");
            migrationBuilder.DropTable("GiftCards");
        }
    }
}
