using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddFurniturePickup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Pfadiname",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "FurniturePickupRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrderNumber = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pfadiname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PickupDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdminNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FurniturePickupRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FurniturePickupRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FurniturePickupImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FurniturePickupRequestId = table.Column<int>(type: "int", nullable: false),
                    ImageData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FurniturePickupImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FurniturePickupImages_FurniturePickupRequests_FurniturePickupRequestId",
                        column: x => x.FurniturePickupRequestId,
                        principalTable: "FurniturePickupRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FurniturePickupImages_FurniturePickupRequestId",
                table: "FurniturePickupImages",
                column: "FurniturePickupRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_FurniturePickupRequests_UserId",
                table: "FurniturePickupRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FurniturePickupImages");

            migrationBuilder.DropTable(
                name: "FurniturePickupRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Pfadiname",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
