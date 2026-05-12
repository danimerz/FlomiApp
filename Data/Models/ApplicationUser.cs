using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace FlomiApp.Data.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public string Pfadiname { get; set; } = string.Empty;

    public string Stufe { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? Birthday { get; set; }
}
