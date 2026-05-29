using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomiApp.Migrations
{
    /// <inheritdoc />
    public partial class AreaTemplateSplit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_AreaCategories_AreaCategoryId",
                table: "Areas");

            migrationBuilder.DropIndex(
                name: "IX_Areas_AreaCategoryId",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "AreaCategoryId",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Areas");

            migrationBuilder.RenameColumn(
                name: "MinAge",
                table: "Areas",
                newName: "AreaTemplateId");

            migrationBuilder.CreateTable(
                name: "AreaTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinAge = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AreaCategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AreaTemplates_AreaCategories_AreaCategoryId",
                        column: x => x.AreaCategoryId,
                        principalTable: "AreaCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Areas_AreaTemplateId",
                table: "Areas",
                column: "AreaTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaTemplates_AreaCategoryId",
                table: "AreaTemplates",
                column: "AreaCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_AreaTemplates_AreaTemplateId",
                table: "Areas",
                column: "AreaTemplateId",
                principalTable: "AreaTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_AreaTemplates_AreaTemplateId",
                table: "Areas");

            migrationBuilder.DropTable(
                name: "AreaTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Areas_AreaTemplateId",
                table: "Areas");

            migrationBuilder.RenameColumn(
                name: "AreaTemplateId",
                table: "Areas",
                newName: "MinAge");

            migrationBuilder.AddColumn<int>(
                name: "AreaCategoryId",
                table: "Areas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Areas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Areas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

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
    }
}
