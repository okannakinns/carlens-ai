using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Carlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManualVehicleAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ListingUrl",
                table: "car_listings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<string>(
                name: "InputType",
                table: "car_listings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Url");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "car_listing_images",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<byte[]>(
                name: "Content",
                table: "car_listing_images",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "car_listing_images",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_car_listing_images_source",
                table: "car_listing_images",
                sql: "((\"Url\" IS NOT NULL AND \"Content\" IS NULL AND \"ContentType\" IS NULL)\nOR (\"Url\" IS NULL AND \"Content\" IS NOT NULL AND \"ContentType\" IS NOT NULL))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_car_listing_images_source",
                table: "car_listing_images");

            migrationBuilder.DropColumn(
                name: "InputType",
                table: "car_listings");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "car_listing_images");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "car_listing_images");

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

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "car_listing_images",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
