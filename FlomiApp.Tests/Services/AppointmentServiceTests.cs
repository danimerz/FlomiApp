using FlomiApp.Data;
using FlomiApp.Data.Models;
using FlomiApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FlomiApp.Tests.Services;

public class AppointmentServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly NoOpMailQueue _mailQueue = new();

    private AppointmentService CreateService(ApplicationDbContext context)
        => new(context, new AreaService(context), _mailQueue,
            new AdminNotificationService(_db.CreateContextFactory(), _mailQueue, NullLogger<AdminNotificationService>.Instance),
            NullLogger<AppointmentService>.Instance);

    private static ApplicationUser MakeUser(string id, DateTime? birthday = null) => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        Email = $"{id}@example.com",
        FirstName = "Test",
        LastName = id,
        Birthday = birthday ?? new DateTime(2000, 1, 1),
        EmailNotificationsEnabled = false
    };

    private async Task<(Event evt, AreaCategory category, AreaTemplate template)> SeedBaseAsync(
        ApplicationDbContext ctx, string categoryName = "Sammeln")
    {
        var evt = new Event { Name = "Flomi", Date = new DateTime(2026, 11, 20), IsActive = true };
        var category = new AreaCategory { Name = categoryName, SortOrder = 1 };
        ctx.Events.Add(evt);
        ctx.AreaCategories.Add(category);
        await ctx.SaveChangesAsync();

        var template = new AreaTemplate { Name = "Geschirr", AreaCategoryId = category.Id };
        ctx.AreaTemplates.Add(template);
        await ctx.SaveChangesAsync();

        return (evt, category, template);
    }

    private static Area MakeArea(Event evt, AreaTemplate template, int capacity,
        string timeSlot = "08:00-12:00", string? altSlot = null, int? altCapacity = null, int minAge = 0)
    {
        template.MinAge = minAge;
        return new Area
        {
            EventId = evt.Id,
            AreaTemplateId = template.Id,
            Date = evt.Date,
            TimeSlot = timeSlot,
            MaxCapacity = capacity,
            AlternativeTimeSlot = altSlot,
            AlternativeMaxCapacity = altCapacity
        };
    }

    public void Dispose() => _db.Dispose();

    // ── Kapazität ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CanRegisterAsync_ReturnsTrue_WhenWithinCapacity()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx);
        var area = MakeArea(evt, template, capacity: 2);
        ctx.Areas.Add(area);
        ctx.Users.Add(MakeUser("u1"));
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).CanRegisterAsync("u1", area.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task CanRegisterAsync_ReturnsFalse_WhenAtCapacity()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx);
        var area = MakeArea(evt, template, capacity: 1);
        ctx.Areas.Add(area);
        ctx.Users.Add(MakeUser("u1"));
        ctx.Users.Add(MakeUser("u2"));
        await ctx.SaveChangesAsync();

        // Bereich mit dem ersten Nutzer voll belegen
        ctx.Appointments.Add(new Appointment { UserId = "u1", AreaId = area.Id, Status = AppointmentStatus.Registered });
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).CanRegisterAsync("u2", area.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task CanRegisterAsync_IgnoresCancelledAppointments_WhenCheckingCapacity()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx);
        var area = MakeArea(evt, template, capacity: 1);
        ctx.Areas.Add(area);
        ctx.Users.Add(MakeUser("u1"));
        ctx.Users.Add(MakeUser("u2"));
        await ctx.SaveChangesAsync();

        // Stornierte Anmeldung darf den Platz nicht blockieren
        ctx.Appointments.Add(new Appointment { UserId = "u1", AreaId = area.Id, Status = AppointmentStatus.Cancelled });
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).CanRegisterAsync("u2", area.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task CanRegisterAsync_UsesAlternativeSlotCapacity_WhenRequested()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx);
        // Hauptslot voll (Kapazität 0), Halbtagsschicht hat eigene Kapazität 1
        var area = MakeArea(evt, template, capacity: 0, altSlot: "08:00-10:00", altCapacity: 1);
        ctx.Areas.Add(area);
        ctx.Users.Add(MakeUser("u1"));
        await ctx.SaveChangesAsync();

        var mainSlot = await CreateService(ctx).CanRegisterAsync("u1", area.Id, useAlternativeSlot: false);
        var altSlot  = await CreateService(ctx).CanRegisterAsync("u1", area.Id, useAlternativeSlot: true);

        Assert.False(mainSlot);
        Assert.True(altSlot);
    }

    // ── Mindestalter ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CanRegisterAsync_ReturnsFalse_WhenBelowMinAge()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx);
        var area = MakeArea(evt, template, capacity: 5, minAge: 16);
        ctx.Areas.Add(area);
        // 10 Jahre alt am Event-Datum
        ctx.Users.Add(MakeUser("kid", birthday: evt.Date.AddYears(-10)));
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).CanRegisterAsync("kid", area.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task RegisterForAppointmentAsync_Throws_WithMindestalterMessage_WhenBelowMinAge()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx);
        var area = MakeArea(evt, template, capacity: 5, minAge: 18);
        ctx.Areas.Add(area);
        ctx.Users.Add(MakeUser("kid", birthday: evt.Date.AddYears(-10)));
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService(ctx).RegisterForAppointmentAsync("kid", area.Id));

        Assert.Contains("Mindestalter", ex.Message);
    }

    // ── Zeitkonflikt ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CanRegisterAsync_ReturnsFalse_WhenOverlappingTimeSlotSameDay()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx);
        var areaMorning   = MakeArea(evt, template, capacity: 5, timeSlot: "08:00-12:00");
        var areaOverlapping = MakeArea(evt, template, capacity: 5, timeSlot: "11:00-15:00");
        ctx.Areas.AddRange(areaMorning, areaOverlapping);
        ctx.Users.Add(MakeUser("u1"));
        await ctx.SaveChangesAsync();

        ctx.Appointments.Add(new Appointment { UserId = "u1", AreaId = areaMorning.Id, Status = AppointmentStatus.Registered });
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).CanRegisterAsync("u1", areaOverlapping.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task CanRegisterAsync_ReturnsTrue_WhenNonOverlappingSameDay()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx);
        var areaMorning   = MakeArea(evt, template, capacity: 5, timeSlot: "08:00-12:00");
        var areaAfternoon = MakeArea(evt, template, capacity: 5, timeSlot: "13:00-17:00");
        ctx.Areas.AddRange(areaMorning, areaAfternoon);
        ctx.Users.Add(MakeUser("u1"));
        await ctx.SaveChangesAsync();

        ctx.Appointments.Add(new Appointment { UserId = "u1", AreaId = areaMorning.Id, Status = AppointmentStatus.Registered });
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).CanRegisterAsync("u1", areaAfternoon.Id);

        Assert.True(result);
    }

    // ── Verkauf: nur ein Bereich pro Event ───────────────────────────────────

    [Fact]
    public async Task CanRegisterAsync_ReturnsFalse_ForSecondVerkaufAreaInSameEvent()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx, categoryName: "Verkauf");
        var firstArea  = MakeArea(evt, template, capacity: 5, timeSlot: "08:00-12:00");
        var secondArea = MakeArea(evt, template, capacity: 5, timeSlot: "13:00-17:00");
        ctx.Areas.AddRange(firstArea, secondArea);
        ctx.Users.Add(MakeUser("u1"));
        await ctx.SaveChangesAsync();

        ctx.Appointments.Add(new Appointment { UserId = "u1", AreaId = firstArea.Id, Status = AppointmentStatus.Registered });
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).CanRegisterAsync("u1", secondArea.Id);

        Assert.False(result);
    }

    // ── Kuchen: nur einmal pro Tag ───────────────────────────────────────────

    [Fact]
    public async Task CanRegisterAsync_ReturnsFalse_ForSecondKuchenAreaSameDay()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx, categoryName: "Kuchen");
        var template2 = new AreaTemplate { Name = "Kuchen Nachmittag", AreaCategoryId = template.AreaCategoryId };
        ctx.AreaTemplates.Add(template2);
        await ctx.SaveChangesAsync();

        var firstArea  = MakeArea(evt, template,  capacity: 5, timeSlot: "08:00-12:00");
        var secondArea = MakeArea(evt, template2, capacity: 5, timeSlot: "13:00-17:00");
        ctx.Areas.AddRange(firstArea, secondArea);
        ctx.Users.Add(MakeUser("u1"));
        await ctx.SaveChangesAsync();

        ctx.Appointments.Add(new Appointment { UserId = "u1", AreaId = firstArea.Id, Status = AppointmentStatus.Registered });
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).CanRegisterAsync("u1", secondArea.Id);

        Assert.False(result);
    }

    // ── Registrierung ────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterForAppointmentAsync_Throws_WithVollMessage_WhenAtCapacity()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx);
        var area = MakeArea(evt, template, capacity: 1);
        ctx.Areas.Add(area);
        ctx.Users.Add(MakeUser("u1"));
        ctx.Users.Add(MakeUser("u2"));
        await ctx.SaveChangesAsync();
        ctx.Appointments.Add(new Appointment { UserId = "u1", AreaId = area.Id, Status = AppointmentStatus.Registered });
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService(ctx).RegisterForAppointmentAsync("u2", area.Id));

        Assert.Equal("Der Bereich ist voll.", ex.Message);
    }

    [Fact]
    public async Task RegisterForAppointmentAsync_CreatesRegisteredAppointment_WhenValid()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx);
        var area = MakeArea(evt, template, capacity: 5);
        ctx.Areas.Add(area);
        ctx.Users.Add(MakeUser("u1"));
        await ctx.SaveChangesAsync();

        await CreateService(ctx).RegisterForAppointmentAsync("u1", area.Id, comment: "Bin etwas später");

        using var verifyCtx = _db.CreateContext();
        var saved = await verifyCtx.Appointments.SingleAsync(a => a.UserId == "u1" && a.AreaId == area.Id);
        Assert.Equal(AppointmentStatus.Registered, saved.Status);
        Assert.Equal("Bin etwas später", saved.Comment);
    }

    // ── Stornierung ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAppointmentAsync_SetsStatusToCancelled()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx);
        var area = MakeArea(evt, template, capacity: 5);
        ctx.Areas.Add(area);
        ctx.Users.Add(MakeUser("u1"));
        await ctx.SaveChangesAsync();
        var appt = new Appointment { UserId = "u1", AreaId = area.Id, Status = AppointmentStatus.Registered };
        ctx.Appointments.Add(appt);
        await ctx.SaveChangesAsync();

        await CreateService(ctx).CancelAppointmentAsync(appt.Id, "u1");

        using var verifyCtx = _db.CreateContext();
        var saved = await verifyCtx.Appointments.SingleAsync(a => a.Id == appt.Id);
        Assert.Equal(AppointmentStatus.Cancelled, saved.Status);
    }

    [Fact]
    public async Task CancelAppointmentAsync_ClearsDriverAssignment_WhenCancellingFahrerAppointment()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, fahrerTemplate) = await SeedBaseAsync(ctx, categoryName: "Fahrer");
        fahrerTemplate.Name = "Fahrer";
        await ctx.SaveChangesAsync();

        var area = MakeArea(evt, fahrerTemplate, capacity: 5, timeSlot: "08:00-12:00");
        ctx.Areas.Add(area);
        ctx.Users.Add(MakeUser("driver1"));
        await ctx.SaveChangesAsync();

        var appt = new Appointment { UserId = "driver1", AreaId = area.Id, Status = AppointmentStatus.Registered };
        ctx.Appointments.Add(appt);

        var vehicle = new Vehicle { OwnerName = "Fahrzeughalter" };
        ctx.Vehicles.Add(vehicle);
        await ctx.SaveChangesAsync();

        var assignment = new VehicleAssignment { VehicleId = vehicle.Id, EventId = evt.Id };
        ctx.VehicleAssignments.Add(assignment);
        await ctx.SaveChangesAsync();

        ctx.AssignmentDates.Add(new AssignmentDate
        {
            AssignmentId = assignment.Id,
            Date = area.Date,
            DriverUserId = "driver1",
            DriverPhone = "079 000 00 00"
        });
        await ctx.SaveChangesAsync();

        await CreateService(ctx).CancelAppointmentAsync(appt.Id, "driver1");

        using var verifyCtx = _db.CreateContext();
        var assignedDate = await verifyCtx.AssignmentDates.SingleAsync(d => d.AssignmentId == assignment.Id);
        Assert.Null(assignedDate.DriverUserId);
        Assert.Null(assignedDate.DriverPhone);
    }

    // ── Spontan (ohne Datum/Zeit) ────────────────────────────────────────────

    [Fact]
    public async Task CanRegisterAsync_SpontaneousArea_DoesNotConflictWithFixedSlotSameDay()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx);
        var fixedArea = MakeArea(evt, template, capacity: 5, timeSlot: "08:00-17:00");
        var spontaneous = MakeArea(evt, template, capacity: 999, timeSlot: "");
        spontaneous.IsSpontaneous = true;
        ctx.Areas.AddRange(fixedArea, spontaneous);
        ctx.Users.Add(MakeUser("u1"));
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        await service.RegisterForAppointmentAsync("u1", fixedArea.Id);

        Assert.True(await service.CanRegisterAsync("u1", spontaneous.Id));
    }

    [Fact]
    public async Task CanRegisterAsync_SpontaneousArea_ReturnsFalse_WhenAlreadyRegistered()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx);
        var spontaneous = MakeArea(evt, template, capacity: 999, timeSlot: "");
        spontaneous.IsSpontaneous = true;
        ctx.Areas.Add(spontaneous);
        ctx.Users.Add(MakeUser("u1"));
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        await service.RegisterForAppointmentAsync("u1", spontaneous.Id);

        Assert.False(await service.CanRegisterAsync("u1", spontaneous.Id));
    }

    [Fact]
    public async Task CanRegisterAsync_SpontaneousArea_AllowsFamilyMemberOfRegisteredUser()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx);
        var spontaneous = MakeArea(evt, template, capacity: 999, timeSlot: "");
        spontaneous.IsSpontaneous = true;
        ctx.Areas.Add(spontaneous);
        ctx.Users.Add(MakeUser("u1"));
        var kid = new FamilyMember { UserId = "u1", FirstName = "Kind", LastName = "u1", Birthday = new DateTime(2015, 1, 1) };
        ctx.FamilyMembers.Add(kid);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        await service.RegisterForAppointmentAsync("u1", spontaneous.Id);

        Assert.True(await service.CanRegisterAsync("u1", spontaneous.Id, kid.Id));
    }

    [Fact]
    public async Task CanRegisterAsync_SpontaneousArea_RespectsCapacity()
    {
        using var ctx = _db.CreateContext();
        var (evt, _, template) = await SeedBaseAsync(ctx);
        var spontaneous = MakeArea(evt, template, capacity: 1, timeSlot: "");
        spontaneous.IsSpontaneous = true;
        ctx.Areas.Add(spontaneous);
        ctx.Users.AddRange(MakeUser("u1"), MakeUser("u2"));
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        await service.RegisterForAppointmentAsync("u1", spontaneous.Id);

        Assert.False(await service.CanRegisterAsync("u2", spontaneous.Id));
    }
}
