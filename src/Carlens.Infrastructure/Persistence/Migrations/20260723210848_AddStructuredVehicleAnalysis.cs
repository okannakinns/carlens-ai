using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Carlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredVehicleAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RiskNotes",
                table: "listing_analyses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyReasoning",
                table: "listing_analyses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfidenceScore",
                table: "listing_analyses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedMarketPriceMax",
                table: "listing_analyses",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedMarketPriceMin",
                table: "listing_analyses",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectionChecklist",
                table: "listing_analyses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KnownIssues",
                table: "listing_analyses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MileageEvaluation",
                table: "listing_analyses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceAssessment",
                table: "listing_analyses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceEvaluation",
                table: "listing_analyses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recommendation",
                table: "listing_analyses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "car_listing_comparables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    ModelYear = table.Column<int>(type: "integer", nullable: true),
                    Mileage = table.Column<int>(type: "integer", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Url = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_car_listing_comparables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_car_listing_comparables_car_listings_CarListingId",
                        column: x => x.CarListingId,
                        principalTable: "car_listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_car_listing_comparables_CarListingId_DisplayOrder",
                table: "car_listing_comparables",
                columns: new[] { "CarListingId", "DisplayOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "car_listing_comparables");

            migrationBuilder.DropColumn(
                name: "BuyReasoning",
                table: "listing_analyses");

            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "listing_analyses");

            migrationBuilder.DropColumn(
                name: "EstimatedMarketPriceMax",
                table: "listing_analyses");

            migrationBuilder.DropColumn(
                name: "EstimatedMarketPriceMin",
                table: "listing_analyses");

            migrationBuilder.DropColumn(
                name: "InspectionChecklist",
                table: "listing_analyses");

            migrationBuilder.DropColumn(
                name: "KnownIssues",
                table: "listing_analyses");

            migrationBuilder.DropColumn(
                name: "MileageEvaluation",
                table: "listing_analyses");

            migrationBuilder.DropColumn(
                name: "PriceAssessment",
                table: "listing_analyses");

            migrationBuilder.DropColumn(
                name: "PriceEvaluation",
                table: "listing_analyses");

            migrationBuilder.DropColumn(
                name: "Recommendation",
                table: "listing_analyses");

            migrationBuilder.AlterColumn<string>(
                name: "RiskNotes",
                table: "listing_analyses",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
