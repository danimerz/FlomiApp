using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CheckInEnabled",
                table: "Events",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedInAt",
                table: "Appointments",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckInEnabled",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                table: "Appointments");
        }
    }
}
