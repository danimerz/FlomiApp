using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomiApp.Migrations
{
    /// <inheritdoc />
    public partial class MakePickupUserIdOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FurniturePickupRequests_AspNetUsers_UserId",
                table: "FurniturePickupRequests");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "FurniturePickupRequests",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_FurniturePickupRequests_AspNetUsers_UserId",
                table: "FurniturePickupRequests",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FurniturePickupRequests_AspNetUsers_UserId",
                table: "FurniturePickupRequests");

            migrationBuilder.UpdateData(
                table: "FurniturePickupRequests",
                keyColumn: "UserId",
                keyValue: null,
                column: "UserId",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "FurniturePickupRequests",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_FurniturePickupRequests_AspNetUsers_UserId",
                table: "FurniturePickupRequests",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
