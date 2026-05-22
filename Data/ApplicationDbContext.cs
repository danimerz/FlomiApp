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
    public DbSet<FamilyMember> FamilyMembers { get; set; }
    public DbSet<FurniturePickupRequest> FurniturePickupRequests { get; set; }
    public DbSet<FurniturePickupImage> FurniturePickupImages { get; set; }
    public DbSet<FurniturePickupSettings> FurniturePickupSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── FurniturePickupSettings Seed ────────────────────────────────────────
        modelBuilder.Entity<FurniturePickupSettings>().HasData(
            new FurniturePickupSettings
            {
                Id             = 1,
                IsEnabled      = false,
                PickupDateFrom = null,
                PickupDateTo   = null
            }
        );
    }
}
