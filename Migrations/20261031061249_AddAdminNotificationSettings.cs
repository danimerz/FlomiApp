using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminNotificationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Standard «an»: wirkt nur für Konten mit Rolle «Admin», die so ab sofort Mails bekommen
            migrationBuilder.AddColumn<bool>(
                name: "NotifyNewUsers",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyRegistrations",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyNewUsers",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NotifyRegistrations",
                table: "AspNetUsers");
        }
    }
}
