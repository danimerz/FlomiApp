using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAcceptsDisposalFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptsDisposalFee",
                table: "FurniturePickupRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_FurniturePickupRequests_EventId",
                table: "FurniturePickupRequests",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_FurniturePickupRequests_Events_EventId",
                table: "FurniturePickupRequests",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FurniturePickupRequests_Events_EventId",
                table: "FurniturePickupRequests");

            migrationBuilder.DropIndex(
                name: "IX_FurniturePickupRequests_EventId",
                table: "FurniturePickupRequests");

            migrationBuilder.DropColumn(
                name: "AcceptsDisposalFee",
                table: "FurniturePickupRequests");
        }
    }
}
