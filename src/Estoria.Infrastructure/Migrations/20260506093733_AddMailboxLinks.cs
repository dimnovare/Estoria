using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Estoria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMailboxLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MailboxLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GraphMessageId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    GraphConversationId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    InternetMessageId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    DealId = table.Column<Guid>(type: "uuid", nullable: true),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FromAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailboxLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailboxLinks_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MailboxLinks_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MailboxLinks_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailboxLinks_ContactId_ReceivedAt",
                table: "MailboxLinks",
                columns: new[] { "ContactId", "ReceivedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_MailboxLinks_DealId_ReceivedAt",
                table: "MailboxLinks",
                columns: new[] { "DealId", "ReceivedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_MailboxLinks_GraphConversationId",
                table: "MailboxLinks",
                column: "GraphConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_MailboxLinks_GraphMessageId",
                table: "MailboxLinks",
                column: "GraphMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MailboxLinks_InternetMessageId",
                table: "MailboxLinks",
                column: "InternetMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MailboxLinks_PropertyId",
                table: "MailboxLinks",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_MailboxLinks_ReceivedAt",
                table: "MailboxLinks",
                column: "ReceivedAt",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailboxLinks");
        }
    }
}
