using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace E_Com.infrastructure.Data.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(E_Com.infrastructure.Data.AppDbContext))]
    [Migration("20260602000004_AddBundlesTiersReservations")]
    public partial class AddBundlesTiersReservations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bundles
            migrationBuilder.CreateTable(
                name: "Bundles",
                columns: table => new
                {
                    Id              = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name            = table.Column<string>(nullable: false),
                    Description     = table.Column<string>(nullable: false, defaultValue: ""),
                    DiscountPercent = table.Column<decimal>(nullable: false, defaultValue: 10m),
                    IsActive        = table.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt       = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table => table.PrimaryKey("PK_Bundles", x => x.Id));

            // BundleItems
            migrationBuilder.CreateTable(
                name: "BundleItems",
                columns: table => new
                {
                    Id        = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BundleId  = table.Column<int>(nullable: false),
                    ProductId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BundleItems", x => x.Id);
                    table.ForeignKey("FK_BundleItems_Bundles",  x => x.BundleId,  "Bundles",  "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_BundleItems_Products", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateIndex("IX_BundleItems_BundleId",  "BundleItems", "BundleId");
            migrationBuilder.CreateIndex("IX_BundleItems_ProductId", "BundleItems", "ProductId");

            // PriceTiers
            migrationBuilder.CreateTable(
                name: "PriceTiers",
                columns: table => new
                {
                    Id          = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId   = table.Column<int>(nullable: false),
                    MinQuantity = table.Column<int>(nullable: false),
                    UnitPrice   = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceTiers", x => x.Id);
                    table.ForeignKey("FK_PriceTiers_Products", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateIndex("IX_PriceTiers_ProductId", "PriceTiers", "ProductId");

            // StockReservations
            migrationBuilder.CreateTable(
                name: "StockReservations",
                columns: table => new
                {
                    Id        = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(nullable: false),
                    BasketId  = table.Column<string>(nullable: false),
                    Quantity  = table.Column<int>(nullable: false),
                    ExpiresAt = table.Column<DateTime>(nullable: false),
                    Released  = table.Column<bool>(nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table => table.PrimaryKey("PK_StockReservations", x => x.Id));
            migrationBuilder.CreateIndex("IX_StockReservations_ProductId", "StockReservations", "ProductId");
            migrationBuilder.CreateIndex("IX_StockReservations_BasketId",  "StockReservations", "BasketId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("StockReservations");
            migrationBuilder.DropTable("PriceTiers");
            migrationBuilder.DropTable("BundleItems");
            migrationBuilder.DropTable("Bundles");
        }
    }
}
