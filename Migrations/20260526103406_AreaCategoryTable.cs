using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FlomiApp.Migrations
{
    /// <inheritdoc />
    public partial class AreaCategoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Category",
                table: "Areas",
                newName: "AreaCategoryId");

            migrationBuilder.CreateTable(
                name: "AreaCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaCategories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AreaCategories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Sammeln" },
                    { 2, "Sortieren" },
                    { 3, "Verkauf" },
                    { 4, "Sonstiges" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Areas_AreaCategoryId",
                table: "Areas",
                column: "AreaCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_AreaCategories_AreaCategoryId",
                table: "Areas",
                column: "AreaCategoryId",
                principalTable: "AreaCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_AreaCategories_AreaCategoryId",
                table: "Areas");

            migrationBuilder.DropTable(
                name: "AreaCategories");

            migrationBuilder.DropIndex(
                name: "IX_Areas_AreaCategoryId",
                table: "Areas");

            migrationBuilder.RenameColumn(
                name: "AreaCategoryId",
                table: "Areas",
                newName: "Category");
        }
    }
}
