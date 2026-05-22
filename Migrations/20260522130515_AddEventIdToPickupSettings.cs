using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddEventIdToPickupSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "FurniturePickupSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "FurniturePickupRequests",
                type: "int",
                nullable: true);

            migrationBuilder.InsertData(
                table: "FurniturePickupSettings",
                columns: new[] { "Id", "EventId", "IsEnabled", "PickupDateFrom", "PickupDateTo" },
                values: new object[] { 1, null, false, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_FurniturePickupSettings_EventId",
                table: "FurniturePickupSettings",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_FurniturePickupSettings_Events_EventId",
                table: "FurniturePickupSettings",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FurniturePickupSettings_Events_EventId",
                table: "FurniturePickupSettings");

            migrationBuilder.DropIndex(
                name: "IX_FurniturePickupSettings_EventId",
                table: "FurniturePickupSettings");

            migrationBuilder.DeleteData(
                table: "FurniturePickupSettings",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "FurniturePickupSettings");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "FurniturePickupRequests");
        }
    }
}
