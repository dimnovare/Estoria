using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Estoria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedSearchesAndHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PropertyEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PreviousJson = table.Column<string>(type: "text", nullable: true),
                    NewJson = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyEvents_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedSearches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PreferredLanguage = table.Column<int>(type: "integer", nullable: false),
                    FilterJson = table.Column<string>(type: "text", nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    LastSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastResultsCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UnsubscribeToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedSearches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedSearches_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyEvents_PropertyId_CreatedAt",
                table: "PropertyEvents",
                columns: new[] { "PropertyId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_SavedSearches_ContactId",
                table: "SavedSearches",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedSearches_Email",
                table: "SavedSearches",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_SavedSearches_IsActive_Frequency",
                table: "SavedSearches",
                columns: new[] { "IsActive", "Frequency" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedSearches_UnsubscribeToken",
                table: "SavedSearches",
                column: "UnsubscribeToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropertyEvents");

            migrationBuilder.DropTable(
                name: "SavedSearches");
        }
    }
}
