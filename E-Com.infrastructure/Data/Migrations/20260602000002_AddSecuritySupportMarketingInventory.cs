using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace E_Com.infrastructure.Data.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(E_Com.infrastructure.Data.AppDbContext))]
    [Migration("20260602000002_AddSecuritySupportMarketingInventory")]
    public partial class AddSecuritySupportMarketingInventory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // OtpCodes
            migrationBuilder.CreateTable(
                name: "OtpCodes",
                columns: table => new
                {
                    Id        = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email     = table.Column<string>(nullable: false),
                    Code      = table.Column<string>(nullable: false),
                    Purpose   = table.Column<string>(nullable: false, defaultValue: "Login"),
                    Used      = table.Column<bool>(nullable: false, defaultValue: false),
                    ExpiresAt = table.Column<DateTime>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table => table.PrimaryKey("PK_OtpCodes", x => x.Id));
            migrationBuilder.CreateIndex("IX_OtpCodes_Email", "OtpCodes", "Email");

            // SupportTickets
            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    Id        = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId    = table.Column<string>(nullable: false),
                    UserEmail = table.Column<string>(nullable: false),
                    Subject   = table.Column<string>(nullable: false),
                    Category  = table.Column<string>(nullable: false, defaultValue: "General"),
                    Status    = table.Column<string>(nullable: false, defaultValue: "Open"),
                    Priority  = table.Column<string>(nullable: false, defaultValue: "Normal"),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table => table.PrimaryKey("PK_SupportTickets", x => x.Id));
            migrationBuilder.CreateIndex("IX_SupportTickets_UserId", "SupportTickets", "UserId");

            // TicketMessages
            migrationBuilder.CreateTable(
                name: "TicketMessages",
                columns: table => new
                {
                    Id        = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TicketId  = table.Column<int>(nullable: false),
                    SenderId  = table.Column<string>(nullable: false),
                    IsAdmin   = table.Column<bool>(nullable: false, defaultValue: false),
                    Body      = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketMessages", x => x.Id);
                    table.ForeignKey("FK_TicketMessages_SupportTickets", x => x.TicketId, "SupportTickets", "Id", onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateIndex("IX_TicketMessages_TicketId", "TicketMessages", "TicketId");

            // FaqItems
            migrationBuilder.CreateTable(
                name: "FaqItems",
                columns: table => new
                {
                    Id        = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Question  = table.Column<string>(nullable: false),
                    Answer    = table.Column<string>(nullable: false),
                    Category  = table.Column<string>(nullable: false, defaultValue: "General"),
                    SortOrder = table.Column<int>(nullable: false, defaultValue: 0),
                    IsActive  = table.Column<bool>(nullable: false, defaultValue: true)
                },
                constraints: table => table.PrimaryKey("PK_FaqItems", x => x.Id));

            // ReferralProfiles
            migrationBuilder.CreateTable(
                name: "ReferralProfiles",
                columns: table => new
                {
                    Id            = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId        = table.Column<string>(nullable: false),
                    Code          = table.Column<string>(nullable: false),
                    TotalReferred = table.Column<int>(nullable: false, defaultValue: 0),
                    PointsEarned  = table.Column<int>(nullable: false, defaultValue: 0),
                    CreatedAt     = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table => table.PrimaryKey("PK_ReferralProfiles", x => x.Id));
            migrationBuilder.CreateIndex("IX_ReferralProfiles_Code", "ReferralProfiles", "Code", unique: true);
            migrationBuilder.CreateIndex("IX_ReferralProfiles_UserId", "ReferralProfiles", "UserId", unique: true);

            // Referrals
            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id             = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferrerUserId = table.Column<string>(nullable: false),
                    ReferredEmail  = table.Column<string>(nullable: false),
                    Code           = table.Column<string>(nullable: false),
                    Status         = table.Column<string>(nullable: false, defaultValue: "Pending"),
                    CreatedAt      = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CompletedAt    = table.Column<DateTime>(nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Referrals", x => x.Id));
            migrationBuilder.CreateIndex("IX_Referrals_ReferrerUserId", "Referrals", "ReferrerUserId");

            // EmailCampaigns
            migrationBuilder.CreateTable(
                name: "EmailCampaigns",
                columns: table => new
                {
                    Id           = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Subject      = table.Column<string>(nullable: false),
                    Body         = table.Column<string>(nullable: false),
                    Segment      = table.Column<string>(nullable: false),
                    Recipients   = table.Column<int>(nullable: false),
                    SentByUserId = table.Column<string>(nullable: false),
                    SentAt       = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table => table.PrimaryKey("PK_EmailCampaigns", x => x.Id));

            // InventoryMovements
            migrationBuilder.CreateTable(
                name: "InventoryMovements",
                columns: table => new
                {
                    Id          = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId   = table.Column<int>(nullable: false),
                    ProductName = table.Column<string>(nullable: false),
                    Change      = table.Column<int>(nullable: false),
                    NewStock    = table.Column<int>(nullable: false),
                    Reason      = table.Column<string>(nullable: false),
                    PerformedBy = table.Column<string>(nullable: true),
                    CreatedAt   = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table => table.PrimaryKey("PK_InventoryMovements", x => x.Id));
            migrationBuilder.CreateIndex("IX_InventoryMovements_ProductId", "InventoryMovements", "ProductId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("InventoryMovements");
            migrationBuilder.DropTable("EmailCampaigns");
            migrationBuilder.DropTable("Referrals");
            migrationBuilder.DropTable("ReferralProfiles");
            migrationBuilder.DropTable("FaqItems");
            migrationBuilder.DropTable("TicketMessages");
            migrationBuilder.DropTable("SupportTickets");
            migrationBuilder.DropTable("OtpCodes");
        }
    }
}
