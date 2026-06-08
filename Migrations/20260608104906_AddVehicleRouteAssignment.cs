using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleRouteAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DepartureAddress",
                table: "FurniturePickupSettings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "AssignedVehicleId",
                table: "FurniturePickupRequests",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "FurniturePickupSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "DepartureAddress",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_FurniturePickupRequests_AssignedVehicleId",
                table: "FurniturePickupRequests",
                column: "AssignedVehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_FurniturePickupRequests_Vehicles_AssignedVehicleId",
                table: "FurniturePickupRequests",
                column: "AssignedVehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FurniturePickupRequests_Vehicles_AssignedVehicleId",
                table: "FurniturePickupRequests");

            migrationBuilder.DropIndex(
                name: "IX_FurniturePickupRequests_AssignedVehicleId",
                table: "FurniturePickupRequests");

            migrationBuilder.DropColumn(
                name: "DepartureAddress",
                table: "FurniturePickupSettings");

            migrationBuilder.DropColumn(
                name: "AssignedVehicleId",
                table: "FurniturePickupRequests");
        }
    }
}
