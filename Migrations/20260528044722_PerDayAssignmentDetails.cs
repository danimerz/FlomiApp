using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomiApp.Migrations
{
    /// <inheritdoc />
    public partial class PerDayAssignmentDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehicleAssignments_AspNetUsers_DriverUserId",
                table: "VehicleAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleAssignments_AspNetUsers_HelperUserId",
                table: "VehicleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_VehicleAssignments_DriverUserId",
                table: "VehicleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_VehicleAssignments_HelperUserId",
                table: "VehicleAssignments");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "VehicleAssignments");

            migrationBuilder.DropColumn(
                name: "DriverPhone",
                table: "VehicleAssignments");

            migrationBuilder.DropColumn(
                name: "DriverUserId",
                table: "VehicleAssignments");

            migrationBuilder.DropColumn(
                name: "HelperName",
                table: "VehicleAssignments");

            migrationBuilder.DropColumn(
                name: "HelperUserId",
                table: "VehicleAssignments");

            migrationBuilder.DropColumn(
                name: "PickedUpBy",
                table: "VehicleAssignments");

            migrationBuilder.DropColumn(
                name: "ReadyFrom",
                table: "VehicleAssignments");

            migrationBuilder.DropColumn(
                name: "ReturnedBy",
                table: "VehicleAssignments");

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "AssignmentDates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverPhone",
                table: "AssignmentDates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverUserId",
                table: "AssignmentDates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HelperName",
                table: "AssignmentDates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HelperUserId",
                table: "AssignmentDates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickedUpBy",
                table: "AssignmentDates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReadyFrom",
                table: "AssignmentDates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnedBy",
                table: "AssignmentDates",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "AssignmentDates");

            migrationBuilder.DropColumn(
                name: "DriverPhone",
                table: "AssignmentDates");

            migrationBuilder.DropColumn(
                name: "DriverUserId",
                table: "AssignmentDates");

            migrationBuilder.DropColumn(
                name: "HelperName",
                table: "AssignmentDates");

            migrationBuilder.DropColumn(
                name: "HelperUserId",
                table: "AssignmentDates");

            migrationBuilder.DropColumn(
                name: "PickedUpBy",
                table: "AssignmentDates");

            migrationBuilder.DropColumn(
                name: "ReadyFrom",
                table: "AssignmentDates");

            migrationBuilder.DropColumn(
                name: "ReturnedBy",
                table: "AssignmentDates");

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "VehicleAssignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverPhone",
                table: "VehicleAssignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverUserId",
                table: "VehicleAssignments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HelperName",
                table: "VehicleAssignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HelperUserId",
                table: "VehicleAssignments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickedUpBy",
                table: "VehicleAssignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReadyFrom",
                table: "VehicleAssignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnedBy",
                table: "VehicleAssignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_DriverUserId",
                table: "VehicleAssignments",
                column: "DriverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_HelperUserId",
                table: "VehicleAssignments",
                column: "HelperUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleAssignments_AspNetUsers_DriverUserId",
                table: "VehicleAssignments",
                column: "DriverUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleAssignments_AspNetUsers_HelperUserId",
                table: "VehicleAssignments",
                column: "HelperUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
