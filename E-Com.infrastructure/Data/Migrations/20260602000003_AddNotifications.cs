using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace E_Com.infrastructure.Data.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(E_Com.infrastructure.Data.AppDbContext))]
    [Migration("20260602000003_AddNotifications")]
    public partial class AddNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id        = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId    = table.Column<string>(nullable: false),
                    Type      = table.Column<string>(nullable: false, defaultValue: "info"),
                    Icon      = table.Column<string>(nullable: false, defaultValue: "notifications"),
                    Title     = table.Column<string>(nullable: false),
                    Message   = table.Column<string>(nullable: false),
                    Link      = table.Column<string>(nullable: true),
                    IsRead    = table.Column<bool>(nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table => table.PrimaryKey("PK_Notifications", x => x.Id));

            migrationBuilder.CreateIndex("IX_Notifications_UserId", "Notifications", "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
            => migrationBuilder.DropTable("Notifications");
    }
}
