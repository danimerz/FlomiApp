using FlomiApp.Data;
using FlomiApp.Data.Models;
using FlomiApp.Security;
using FlomiApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FlomiApp.Tests.Services;

public class AdminNotificationServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly NoOpMailQueue _mailQueue = new();

    public void Dispose() => _db.Dispose();

    private AdminNotificationService CreateService() =>
        new(_db.CreateContextFactory(), _mailQueue, NullLogger<AdminNotificationService>.Instance);

    private AppointmentService CreateAppointmentService(ApplicationDbContext ctx) =>
        new(ctx, new AreaService(ctx), _mailQueue, CreateService(), NullLogger<AppointmentService>.Instance);

    private static ApplicationUser MakeUser(string id, bool confirmed = true) => new()
    {
        Id = id, UserName = $"{id}@example.com", Email = $"{id}@example.com", EmailConfirmed = confirmed,
        FirstName = "Vorname", LastName = id, Stufe = "Pios", PhoneNumber = "079 123 45 67",
        Birthday = new DateTime(2000, 5, 1), EmailNotificationsEnabled = false,
    };

    private async Task SeedAdminsAsync(params ApplicationUser[] admins)
    {
        using var ctx = _db.CreateContext();
        var role = new IdentityRole(Roles.Admin) { NormalizedName = Roles.Admin.ToUpperInvariant() };
        ctx.Roles.Add(role);
        foreach (var admin in admins)
        {
            ctx.Users.Add(admin);
            ctx.UserRoles.Add(new IdentityUserRole<string> { UserId = admin.Id, RoleId = role.Id });
        }
        await ctx.SaveChangesAsync();
    }

    private async Task AddUserAsync(ApplicationUser user)
    {
        using var ctx = _db.CreateContext();
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task NewUser_GoesOnlyToConfirmedAdminsWithNotificationOn()
    {
        var off = MakeUser("admin-off");
        off.NotifyNewUsers = false;
        await SeedAdminsAsync(MakeUser("admin-on"), off, MakeUser("admin-unconfirmed", confirmed: false));
        await AddUserAsync(MakeUser("helfer"));                     // kein Admin
        await AddUserAsync(MakeUser("neu", confirmed: false));

        await CreateService().NotifyNewUserAsync("neu", "Google");

        var sent = (await _mailQueue.RunAllAsync()).NewUserMails;
        var (to, mail) = Assert.Single(sent);
        Assert.Equal("admin-on@example.com", to);
        Assert.Equal("Vorname neu", mail.FullName);
        Assert.Equal("neu@example.com", mail.Email);
        Assert.False(mail.EmailConfirmed);
        Assert.Equal("Google", mail.Method);
        Assert.Equal("079 123 45 67", mail.PhoneNumber);
        Assert.Equal(5, mail.TotalUsers);
        Assert.Null(mail.AdminUrl); // ohne Anfrage kein Link
    }

    [Fact]
    public async Task NoAdmins_SendsNothing()
    {
        await AddUserAsync(MakeUser("neu"));

        await CreateService().NotifyNewUserAsync("neu", "E-Mail und Passwort");

        Assert.Equal(0, _mailQueue.EnqueueCount);
    }

    [Fact]
    public async Task Registration_AndCancellation_CarryDetails()
    {
        var newUsersOnly = MakeUser("admin-konten");
        newUsersOnly.NotifyRegistrations = false;
        await SeedAdminsAsync(MakeUser("admin"), newUsersOnly);
        await AddUserAsync(MakeUser("mama"));

        int areaId, memberId;
        using (var ctx = _db.CreateContext())
        {
            var evt      = new Event { Name = "Flomi Herbst", Date = new DateTime(2026, 11, 20), IsActive = true };
            var category = new AreaCategory { Name = "Sammeln", SortOrder = 1 };
            ctx.Events.Add(evt);
            ctx.AreaCategories.Add(category);
            await ctx.SaveChangesAsync();

            var template = new AreaTemplate { Name = "Geschirr", AreaCategoryId = category.Id };
            ctx.AreaTemplates.Add(template);
            await ctx.SaveChangesAsync();

            var area = new Area
            {
                EventId = evt.Id, AreaTemplateId = template.Id, Date = evt.Date,
                TimeSlot = "08:00-12:00", MaxCapacity = 4,
            };
            var member = new FamilyMember
            {
                UserId = "mama", FirstName = "Lena", LastName = "Muster", Stufe = "Wölfe",
                Birthday = new DateTime(2016, 3, 2),
            };
            ctx.Areas.Add(area);
            ctx.FamilyMembers.Add(member);
            await ctx.SaveChangesAsync();
            areaId   = area.Id;
            memberId = member.Id;
        }

        using (var ctx = _db.CreateContext())
            await CreateAppointmentService(ctx).RegisterForAppointmentAsync("mama", areaId, memberId, "Bringe Kisten mit");

        int appointmentId;
        using (var ctx = _db.CreateContext())
            appointmentId = ctx.Appointments.Single().Id;

        using (var ctx = _db.CreateContext())
            await CreateAppointmentService(ctx).CancelAppointmentAsync(appointmentId, "mama");

        var sent = (await _mailQueue.RunAllAsync()).RegistrationMails;
        Assert.Equal(2, sent.Count);
        Assert.All(sent, s => Assert.Equal("admin@example.com", s.To));

        var registration = sent[0].Mail;
        Assert.False(registration.Cancelled);
        Assert.Equal("Lena Muster", registration.PersonName);
        Assert.Equal("Wölfe", registration.PersonStufe);
        Assert.True(registration.ForFamilyMember);
        Assert.Equal("Vorname mama", registration.AccountName);
        Assert.Equal("mama@example.com", registration.AccountEmail);
        Assert.Equal("Flomi Herbst", registration.EventName);
        Assert.Equal("Geschirr", registration.AreaName);
        Assert.Equal("Sammeln", registration.CategoryName);
        Assert.Equal("08:00-12:00", registration.TimeSlot);
        Assert.Equal("Bringe Kisten mit", registration.Comment);
        Assert.Equal(1, registration.Registered);
        Assert.Equal(4, registration.Capacity);
        Assert.Equal(1, registration.AccountRegistrationsInEvent);

        var cancellation = sent[1].Mail;
        Assert.True(cancellation.Cancelled);
        Assert.Equal(0, cancellation.Registered);
        Assert.Equal(0, cancellation.AccountRegistrationsInEvent);
    }
}
