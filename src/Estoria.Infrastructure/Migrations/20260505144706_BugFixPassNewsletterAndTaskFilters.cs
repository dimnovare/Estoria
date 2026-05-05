using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Estoria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BugFixPassNewsletterAndTaskFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UnsubscribeToken",
                table: "NewsletterSubscribers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            // Backfill before the unique index lands — every legacy row would
            // otherwise share the empty-string default and the constraint
            // would refuse to apply. md5(random|id) yields 32 hex chars per
            // row, which is below the 64-char cap and unique enough; the
            // application rotates these on next subscribe via the regular
            // GenerateToken() path.
            migrationBuilder.Sql("""
                UPDATE "NewsletterSubscribers"
                SET "UnsubscribeToken" = md5(random()::text || clock_timestamp()::text || "Id"::text)
                WHERE "UnsubscribeToken" = '';
            """);

            migrationBuilder.CreateTable(
                name: "NewsletterCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: false),
                    LanguageFilter = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecipientsCount = table.Column<int>(type: "integer", nullable: false),
                    SuccessCount = table.Column<int>(type: "integer", nullable: false),
                    FailureCount = table.Column<int>(type: "integer", nullable: false),
                    SentByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterCampaigns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscribers_UnsubscribeToken",
                table: "NewsletterSubscribers",
                column: "UnsubscribeToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterCampaigns_CreatedAt",
                table: "NewsletterCampaigns",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterCampaigns_Status",
                table: "NewsletterCampaigns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsletterCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_NewsletterSubscribers_UnsubscribeToken",
                table: "NewsletterSubscribers");

            migrationBuilder.DropColumn(
                name: "UnsubscribeToken",
                table: "NewsletterSubscribers");
        }
    }
}
