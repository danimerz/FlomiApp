using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomiApp.Migrations
{
    /// <inheritdoc />
    public partial class FixFurniturePickupAutoIncrement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MySQL setzt AUTO_INCREMENT auf max(N, max_existing_id + 1),
            // d.h. dieser Wert ist sicher auch wenn bereits IDs > 1000 existieren.
            migrationBuilder.Sql("ALTER TABLE `FurniturePickupRequests` AUTO_INCREMENT = 1000;");
            migrationBuilder.Sql("ALTER TABLE `FurniturePickupImages` AUTO_INCREMENT = 1000;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
