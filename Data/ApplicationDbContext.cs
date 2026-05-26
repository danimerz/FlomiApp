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

    public DbSet<Area>                   Areas                   { get; set; }
    public DbSet<Appointment>            Appointments            { get; set; }
    public DbSet<Event>                  Events                  { get; set; }
    public DbSet<FamilyMember>           FamilyMembers           { get; set; }
    public DbSet<FurniturePickupRequest> FurniturePickupRequests { get; set; }
    public DbSet<FurniturePickupImage>   FurniturePickupImages   { get; set; }
    public DbSet<FurniturePickupSettings> FurniturePickupSettings { get; set; }

    // ── NEU ───────────────────────────────────────────────────────────────────
    public DbSet<AreaCategory> AreaCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── FurniturePickupSettings → Event (optional) ──────────────────────
        modelBuilder.Entity<FurniturePickupSettings>()
            .HasOne(s => s.Event)
            .WithMany()
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── AreaCategory → Area (Cascade) ────────────────────────────────────
        modelBuilder.Entity<Area>()
            .HasOne(a => a.AreaCategory)
            .WithMany(c => c.Areas)
            .HasForeignKey(a => a.AreaCategoryId)
            .OnDelete(DeleteBehavior.Restrict); // 👈 kein versehentliches Löschen

        // ── Seed: AreaCategories ─────────────────────────────────────────────
        modelBuilder.Entity<AreaCategory>().HasData(
            new AreaCategory { Id = 1, Name = "Sammeln"   },
            new AreaCategory { Id = 2, Name = "Sortieren" },
            new AreaCategory { Id = 3, Name = "Verkauf"   },
            new AreaCategory { Id = 4, Name = "Sonstiges" }
        );

        // ── Seed: FurniturePickupSettings ────────────────────────────────────
        modelBuilder.Entity<FurniturePickupSettings>().HasData(
            new FurniturePickupSettings
            {
                Id             = 1,
                IsEnabled      = false,
                PickupDateFrom = null,
                PickupDateTo   = null,
                EventId        = null
            }
        );
    }
}