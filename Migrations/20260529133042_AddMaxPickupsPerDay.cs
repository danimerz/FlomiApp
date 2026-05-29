using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxPickupsPerDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxPickupsPerDay",
                table: "FurniturePickupSettings",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "FurniturePickupSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "MaxPickupsPerDay",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxPickupsPerDay",
                table: "FurniturePickupSettings");
        }
    }
}
