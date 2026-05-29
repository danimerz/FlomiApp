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
    public DbSet<AreaTemplate>           AreaTemplates           { get; set; }
    public DbSet<AreaCategory>           AreaCategories          { get; set; }
    public DbSet<Appointment>            Appointments            { get; set; }
    public DbSet<Event>                  Events                  { get; set; }
    public DbSet<FamilyMember>           FamilyMembers           { get; set; }
    public DbSet<FurniturePickupRequest> FurniturePickupRequests { get; set; }
    public DbSet<FurniturePickupImage>   FurniturePickupImages   { get; set; }
    public DbSet<FurniturePickupSettings> FurniturePickupSettings { get; set; }
    public DbSet<Vehicle>           Vehicles           { get; set; }
    public DbSet<VehicleAssignment> VehicleAssignments { get; set; }
    public DbSet<AssignmentDate>    AssignmentDates    { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Vehicle Settings ──────────────────────────────────────────────────
        modelBuilder.Entity<VehicleAssignment>()
            .HasOne(a => a.Vehicle)
            .WithMany(v => v.Assignments)
            .HasForeignKey(a => a.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VehicleAssignment>()
            .HasOne(a => a.Event)
            .WithMany()
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssignmentDate>()
            .HasOne(d => d.Assignment)
            .WithMany(a => a.AssignedDates)
            .HasForeignKey(d => d.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FurniturePickupSettings → Event (optional) ────────────────────────
        modelBuilder.Entity<FurniturePickupSettings>()
            .HasOne(s => s.Event)
            .WithMany()
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── AreaTemplate → AreaCategory ───────────────────────────────────────
        modelBuilder.Entity<AreaTemplate>()
            .HasOne(t => t.AreaCategory)
            .WithMany(c => c.AreaTemplates)
            .HasForeignKey(t => t.AreaCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Area (Zuweisung) → AreaTemplate ──────────────────────────────────
        modelBuilder.Entity<Area>()
            .HasOne(a => a.AreaTemplate)
            .WithMany(t => t.Areas)
            .HasForeignKey(a => a.AreaTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Seed: AreaCategories ──────────────────────────────────────────────
        modelBuilder.Entity<AreaCategory>().HasData(
            new AreaCategory { Id = 1, Name = "Sammeln",   SortOrder = 1 },
            new AreaCategory { Id = 2, Name = "Sortieren", SortOrder = 2 },
            new AreaCategory { Id = 3, Name = "Verkauf",   SortOrder = 3 },
            new AreaCategory { Id = 4, Name = "Sonstiges", SortOrder = 4 }
        );

        // ── Seed: FurniturePickupSettings ─────────────────────────────────────
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
