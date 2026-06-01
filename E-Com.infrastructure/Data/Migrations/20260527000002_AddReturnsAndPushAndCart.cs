using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace E_Com.infrastructure.Data.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(E_Com.infrastructure.Data.AppDbContext))]
    [Migration("20260527000002_AddReturnsAndPushAndCart")]
    public partial class AddReturnsAndPushAndCart : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ReturnRequests
            migrationBuilder.CreateTable(
                name: "ReturnRequests",
                columns: table => new
                {
                    Id          = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId      = table.Column<string>(nullable: false),
                    UserEmail   = table.Column<string>(nullable: false),
                    OrderId     = table.Column<int>(nullable: false),
                    Reason      = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: false, defaultValue: ""),
                    Status      = table.Column<string>(nullable: false, defaultValue: "Pending"),
                    AdminNote   = table.Column<string>(nullable: false, defaultValue: ""),
                    CreatedAt   = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt   = table.Column<DateTime>(nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_ReturnRequests", x => x.Id));

            migrationBuilder.CreateIndex("IX_ReturnRequests_UserId",  "ReturnRequests", "UserId");
            migrationBuilder.CreateIndex("IX_ReturnRequests_OrderId", "ReturnRequests", "OrderId");

            // AbandonedCartTrackers
            migrationBuilder.CreateTable(
                name: "AbandonedCartTrackers",
                columns: table => new
                {
                    Id              = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserEmail       = table.Column<string>(nullable: false),
                    BasketId        = table.Column<string>(nullable: false),
                    PaymentIntentId = table.Column<string>(nullable: true),
                    CreatedAt       = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    EmailSent       = table.Column<bool>(nullable: false, defaultValue: false),
                    EmailSentAt     = table.Column<DateTime>(nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_AbandonedCartTrackers", x => x.Id));

            migrationBuilder.CreateIndex("IX_AbandonedCartTrackers_UserEmail", "AbandonedCartTrackers", "UserEmail");

            // PushSubscriptions
            migrationBuilder.CreateTable(
                name: "PushSubscriptions",
                columns: table => new
                {
                    Id        = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId    = table.Column<string>(nullable: false),
                    Endpoint  = table.Column<string>(nullable: false),
                    P256dh    = table.Column<string>(nullable: false),
                    Auth      = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table => table.PrimaryKey("PK_PushSubscriptions", x => x.Id));

            migrationBuilder.CreateIndex("IX_PushSubscriptions_UserId", "PushSubscriptions", "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("PushSubscriptions");
            migrationBuilder.DropTable("AbandonedCartTrackers");
            migrationBuilder.DropTable("ReturnRequests");
        }
    }
}
