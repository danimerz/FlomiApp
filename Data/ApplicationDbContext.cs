using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FlomiApp.Data.Models;

namespace FlomiApp.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Area> Areas { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Event> Events { get; set; }
}
