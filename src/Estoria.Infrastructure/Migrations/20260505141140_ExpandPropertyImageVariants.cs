using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Estoria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPropertyImageVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "PropertyImages",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "LargeUrl",
                table: "PropertyImages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediumUrl",
                table: "PropertyImages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalKey",
                table: "PropertyImages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProcessingError",
                table: "PropertyImages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessingStatus",
                table: "PropertyImages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ThumbUrl",
                table: "PropertyImages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // Backfill: existing rows uploaded before the two-bucket pipeline
            // already have a usable Url. Mark them processed and point every
            // variant URL at the legacy Url so the public site keeps working
            // unchanged. ProcessingStatus.Done = 2.
            migrationBuilder.Sql("""
                UPDATE "PropertyImages"
                SET "ThumbUrl"         = "Url",
                    "MediumUrl"        = "Url",
                    "LargeUrl"         = "Url",
                    "ProcessingStatus" = 2
                WHERE "Url" IS NOT NULL AND "Url" <> '';
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LargeUrl",
                table: "PropertyImages");

            migrationBuilder.DropColumn(
                name: "MediumUrl",
                table: "PropertyImages");

            migrationBuilder.DropColumn(
                name: "OriginalKey",
                table: "PropertyImages");

            migrationBuilder.DropColumn(
                name: "ProcessingError",
                table: "PropertyImages");

            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "PropertyImages");

            migrationBuilder.DropColumn(
                name: "ThumbUrl",
                table: "PropertyImages");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "PropertyImages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "");
        }
    }
}
