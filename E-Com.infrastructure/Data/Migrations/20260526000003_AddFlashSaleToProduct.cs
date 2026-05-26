using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using E_Com.infrastructure.Data;

#nullable disable

namespace E_Com.infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260526000003_AddFlashSaleToProduct")]
    public partial class AddFlashSaleToProduct : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "Products",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaleEndDate",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SalePrice", table: "Products");
            migrationBuilder.DropColumn(name: "SaleEndDate", table: "Products");
        }
    }
}
