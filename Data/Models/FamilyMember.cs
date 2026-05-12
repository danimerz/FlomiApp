using System.ComponentModel.DataAnnotations;

namespace FlomiApp.Data.Models;

public class FamilyMember
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public string Pfadiname { get; set; } = string.Empty;

    [Required]
    public string Stufe { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime Birthday { get; set; }
}
