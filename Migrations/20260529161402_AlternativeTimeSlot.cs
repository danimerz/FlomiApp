using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomiApp.Migrations
{
    /// <inheritdoc />
    public partial class AlternativeTimeSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MandatoryStufen",
                table: "AreaTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AlternativeMaxCapacity",
                table: "Areas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlternativeTimeSlot",
                table: "Areas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseAlternativeSlot",
                table: "Appointments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MandatoryStufen",
                table: "AreaTemplates");

            migrationBuilder.DropColumn(
                name: "AlternativeMaxCapacity",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "AlternativeTimeSlot",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "UseAlternativeSlot",
                table: "Appointments");
        }
    }
}
