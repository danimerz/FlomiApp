using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutePlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSample",
                table: "Vehicles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GeocodedAddress",
                table: "FurniturePickupRequests",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "FurniturePickupRequests",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "FurniturePickupRequests",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RouteStopOrder",
                table: "FurniturePickupRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSample",
                table: "Events",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSample",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "GeocodedAddress",
                table: "FurniturePickupRequests");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "FurniturePickupRequests");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "FurniturePickupRequests");

            migrationBuilder.DropColumn(
                name: "RouteStopOrder",
                table: "FurniturePickupRequests");

            migrationBuilder.DropColumn(
                name: "IsSample",
                table: "Events");
        }
    }
}
