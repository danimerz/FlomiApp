using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorySortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "AreaCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AreaCategories",
                keyColumn: "Id",
                keyValue: 1,
                column: "SortOrder",
                value: 1);

            migrationBuilder.UpdateData(
                table: "AreaCategories",
                keyColumn: "Id",
                keyValue: 2,
                column: "SortOrder",
                value: 2);

            migrationBuilder.UpdateData(
                table: "AreaCategories",
                keyColumn: "Id",
                keyValue: 3,
                column: "SortOrder",
                value: 3);

            migrationBuilder.UpdateData(
                table: "AreaCategories",
                keyColumn: "Id",
                keyValue: 4,
                column: "SortOrder",
                value: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "AreaCategories");
        }
    }
}
