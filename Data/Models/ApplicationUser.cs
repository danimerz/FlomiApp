using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace FlomiApp.Data.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public string? Pfadiname { get; set; }

    public string Stufe { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? Birthday { get; set; }

    public ICollection<FamilyMember> FamilyMembers { get; set; } = new List<FamilyMember>();

    public bool    EmailNotificationsEnabled { get; set; } = true;
    public string? PreferredTheme            { get; set; }

    // Zwingt zur Passwortänderung beim nächsten Login (z.B. für den
    // automatisch generierten Admin-Passwort bei der Erstinstallation).
    public bool MustChangePassword { get; set; } = false;

    // Einmaliges Einführungs-Panel auf der Anmeldungsseite — true sobald
    // die Person es einmal weggeklickt hat, dann nie wieder angezeigt.
    public bool HasSeenRegisterOnboarding { get; set; } = false;
}