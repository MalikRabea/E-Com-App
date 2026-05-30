using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace E_Com.infrastructure.Data.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(E_Com.infrastructure.Data.AppDbContext))]
    [Migration("20260527000001_AddLoyaltyAndVariants")]
    public partial class AddLoyaltyAndVariants : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // LoyaltyAccounts
            migrationBuilder.CreateTable(
                name: "LoyaltyAccounts",
                columns: table => new
                {
                    Id        = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId    = table.Column<string>(nullable: false),
                    Points    = table.Column<int>(nullable: false, defaultValue: 0),
                    Tier      = table.Column<string>(nullable: false, defaultValue: "Bronze"),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table => table.PrimaryKey("PK_LoyaltyAccounts", x => x.Id));

            migrationBuilder.CreateIndex("IX_LoyaltyAccounts_UserId", "LoyaltyAccounts", "UserId", unique: true);

            // PointsTransactions
            migrationBuilder.CreateTable(
                name: "PointsTransactions",
                columns: table => new
                {
                    Id               = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LoyaltyAccountId = table.Column<int>(nullable: false),
                    Points           = table.Column<int>(nullable: false),
                    Type             = table.Column<int>(nullable: false),
                    Description      = table.Column<string>(nullable: false, defaultValue: ""),
                    OrderId          = table.Column<int>(nullable: true),
                    CreatedAt        = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsTransactions", x => x.Id);
                    table.ForeignKey("FK_PointsTransactions_LoyaltyAccounts",
                        x => x.LoyaltyAccountId, "LoyaltyAccounts", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_PointsTransactions_LoyaltyAccountId",
                "PointsTransactions", "LoyaltyAccountId");

            // ProductVariants
            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id        = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(nullable: false),
                    Type      = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey("FK_ProductVariants_Products",
                        x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_ProductVariants_ProductId", "ProductVariants", "ProductId");

            // VariantOptions
            migrationBuilder.CreateTable(
                name: "VariantOptions",
                columns: table => new
                {
                    Id              = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VariantId       = table.Column<int>(nullable: false),
                    Value           = table.Column<string>(nullable: false),
                    Stock           = table.Column<int>(nullable: false, defaultValue: 0),
                    PriceAdjustment = table.Column<decimal>(nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantOptions", x => x.Id);
                    table.ForeignKey("FK_VariantOptions_ProductVariants",
                        x => x.VariantId, "ProductVariants", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_VariantOptions_VariantId", "VariantOptions", "VariantId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("VariantOptions");
            migrationBuilder.DropTable("ProductVariants");
            migrationBuilder.DropTable("PointsTransactions");
            migrationBuilder.DropTable("LoyaltyAccounts");
        }
    }
}
