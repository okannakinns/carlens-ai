using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Carlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListingSourceAndUsageMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnalyzedImageCount",
                table: "listing_analyses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedCostUsd",
                table: "listing_analyses",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InputTokens",
                table: "listing_analyses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OutputTokens",
                table: "listing_analyses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "car_listings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Model",
                table: "car_listings",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "ListingUrl",
                table: "car_listings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DamageInformation",
                table: "car_listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "car_listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalListingId",
                table: "car_listings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImportError",
                table: "car_listings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ImportedAtUtc",
                table: "car_listings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "car_listings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Series",
                table: "car_listings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceStatus",
                table: "car_listings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Imported");

            migrationBuilder.CreateTable(
                name: "car_listing_images",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_car_listing_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_car_listing_images_car_listings_CarListingId",
                        column: x => x.CarListingId,
                        principalTable: "car_listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "car_listing_specifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_car_listing_specifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_car_listing_specifications_car_listings_CarListingId",
                        column: x => x.CarListingId,
                        principalTable: "car_listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_car_listings_ExternalListingId",
                table: "car_listings",
                column: "ExternalListingId");

            migrationBuilder.CreateIndex(
                name: "IX_car_listing_images_CarListingId_DisplayOrder",
                table: "car_listing_images",
                columns: new[] { "CarListingId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_car_listing_specifications_CarListingId_DisplayOrder",
                table: "car_listing_specifications",
                columns: new[] { "CarListingId", "DisplayOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "car_listing_images");

            migrationBuilder.DropTable(
                name: "car_listing_specifications");

            migrationBuilder.DropIndex(
                name: "IX_car_listings_ExternalListingId",
                table: "car_listings");

            migrationBuilder.DropColumn(
                name: "AnalyzedImageCount",
                table: "listing_analyses");

            migrationBuilder.DropColumn(
                name: "EstimatedCostUsd",
                table: "listing_analyses");

            migrationBuilder.DropColumn(
                name: "InputTokens",
                table: "listing_analyses");

            migrationBuilder.DropColumn(
                name: "OutputTokens",
                table: "listing_analyses");

            migrationBuilder.DropColumn(
                name: "DamageInformation",
                table: "car_listings");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "car_listings");

            migrationBuilder.DropColumn(
                name: "ExternalListingId",
                table: "car_listings");

            migrationBuilder.DropColumn(
                name: "ImportError",
                table: "car_listings");

            migrationBuilder.DropColumn(
                name: "ImportedAtUtc",
                table: "car_listings");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "car_listings");

            migrationBuilder.DropColumn(
                name: "Series",
                table: "car_listings");

            migrationBuilder.DropColumn(
                name: "SourceStatus",
                table: "car_listings");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "car_listings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "Model",
                table: "car_listings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "ListingUrl",
                table: "car_listings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);
        }
    }
}
