using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using E_Com.infrastructure.Data;

#nullable disable

namespace E_Com.infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260526000002_AddCoupons")]
    public partial class AddCoupons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy",
                            Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code             = table.Column<string>(type: "text", nullable: false),
                    DiscountPercent  = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxUses          = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    CurrentUses      = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ExpiryDate       = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive         = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                },
                constraints: table => table.PrimaryKey("PK_Coupons", x => x.Id));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Coupons");
        }
    }
}
