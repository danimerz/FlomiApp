using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomiApp.Migrations
{
    /// <inheritdoc />
    public partial class ConfirmExistingUserEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Registrierung verlangt ab jetzt eine bestätigte E-Mail-Adresse
            // (RequireConfirmedAccount = true). Ohne diesen einmaligen Schritt
            // würden alle bereits bestehenden Konten beim nächsten Login
            // ausgesperrt, weil ihr EmailConfirmed-Flag nie gesetzt wurde.
            // Sie gelten hiermit als "grandfathered" — nur neue Registrierungen
            // ab jetzt müssen den Bestätigungslink tatsächlich anklicken.
            migrationBuilder.Sql("UPDATE `AspNetUsers` SET `EmailConfirmed` = 1 WHERE `EmailConfirmed` = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Absichtlich kein Rollback: Wir können nicht mehr wissen, wessen
            // E-Mail vor dieser Migration tatsächlich schon bestätigt war.
        }
    }
}
