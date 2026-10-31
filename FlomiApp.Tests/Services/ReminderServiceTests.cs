using FlomiApp.Data.Models;
using FlomiApp.Helpers;
using FlomiApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FlomiApp.Tests.Services;

public class ReminderServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly NoOpMailQueue _mailQueue = new();

    // Einsatz am Samstag 10.04.2027; Sommerzeit, Schweiz = UTC+2
    private static readonly DateTime EventDay   = new(2027, 4, 10);
    private static readonly DateTime EveBefore  = new(2027, 4, 9, 15, 0, 0, DateTimeKind.Utc);  // 17:00 Schweiz
    private static readonly DateTime EveAfter   = new(2027, 4, 9, 16, 5, 0, DateTimeKind.Utc);  // 18:05 Schweiz

    public void Dispose() => _db.Dispose();

    private ReminderService CreateService() => new(
        _db.CreateContextFactory(), _mailQueue,
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["App:PublicUrl"] = "https://pfadiflomi.ch" }).Build(),
        NullLogger<ReminderService>.Instance);

    private static ApplicationUser MakeUser(string id, bool notifications = true) => new()
    {
        Id = id, UserName = $"{id}@example.com", Email = $"{id}@example.com", FirstName = id, LastName = "Muster",
        EmailNotificationsEnabled = notifications,
    };

    private sealed record Seed(Event Event, Area Collect, Area Drive, Area Sale, Area Spontaneous, Area NextDay);

    private async Task<Seed> SeedAsync(bool remindersEnabled = true, bool isSample = false, bool checkIn = true)
    {
        using var ctx = _db.CreateContext();
        var evt = new Event { Name = "Flomi 2027", Date = EventDay.AddDays(7), IsActive = true,
                              RemindersEnabled = remindersEnabled, IsSample = isSample, CheckInEnabled = checkIn };
        var sammeln = new AreaCategory { Name = "Sammeln", SortOrder = 1 };
        var fahrer  = new AreaCategory { Name = "Fahrer", SortOrder = 2 };
        var verkauf = new AreaCategory { Name = "Verkauf", SortOrder = 3 };
        ctx.Events.Add(evt);
        ctx.AreaCategories.AddRange(sammeln, fahrer, verkauf);
        await ctx.SaveChangesAsync();

        var tSammeln = new AreaTemplate { Name = "Sammeln", AreaCategoryId = sammeln.Id, Location = "Ägerihalle" };
        var tFahrer  = new AreaTemplate { Name = "Fahrer",  AreaCategoryId = fahrer.Id,  Location = "Werkhof" };
        var tKleider = new AreaTemplate { Name = "Kleider", AreaCategoryId = verkauf.Id };
        ctx.AreaTemplates.AddRange(tSammeln, tFahrer, tKleider);
        await ctx.SaveChangesAsync();

        Area MakeArea(AreaTemplate t, DateTime date, string slot, bool spontaneous = false) => new()
        {
            EventId = evt.Id, AreaTemplateId = t.Id, Date = date, TimeSlot = slot, MaxCapacity = 10, IsSpontaneous = spontaneous,
        };
        var seed = new Seed(evt,
            MakeArea(tSammeln, EventDay, "13:00-17:00"),
            MakeArea(tFahrer,  EventDay, "08:00-12:00"),
            MakeArea(tKleider, EventDay, "09:00-12:00"),
            MakeArea(tSammeln, EventDay, "", spontaneous: true),
            MakeArea(tSammeln, EventDay.AddDays(1), "09:00-17:00"));
        ctx.Areas.AddRange(seed.Collect, seed.Drive, seed.Sale, seed.Spontaneous, seed.NextDay);
        await ctx.SaveChangesAsync();
        return seed;
    }

    private async Task<Appointment> RegisterAsync(string userId, Area area, int? familyMemberId = null, string? comment = null)
    {
        using var ctx = _db.CreateContext();
        var apt = new Appointment { UserId = userId, AreaId = area.Id, FamilyMemberId = familyMemberId,
                                    Comment = comment, Status = AppointmentStatus.Registered };
        ctx.Appointments.Add(apt);
        await ctx.SaveChangesAsync();
        return apt;
    }

    private async Task AddUsersAsync(params ApplicationUser[] users)
    {
        using var ctx = _db.CreateContext();
        ctx.Users.AddRange(users);
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task BeforeSixPm_SendsNothing()
    {
        var seed = await SeedAsync();
        await AddUsersAsync(MakeUser("dani"));
        await RegisterAsync("dani", seed.Collect);

        Assert.Equal(0, await CreateService().SendDueRemindersAsync(EveBefore));
        Assert.Equal(0, _mailQueue.EnqueueCount);
    }

    [Fact]
    public async Task AfterSixPm_OneMailPerAccount_WithAllItemsOfTomorrow_AndOnlyOnce()
    {
        var seed = await SeedAsync();
        await AddUsersAsync(MakeUser("dani"));
        int lenaId;
        using (var ctx = _db.CreateContext())
        {
            var lena = new FamilyMember { UserId = "dani", FirstName = "Lena", LastName = "Muster", Stufe = "Wolf", Birthday = new DateTime(2016, 1, 1) };
            ctx.FamilyMembers.Add(lena);
            await ctx.SaveChangesAsync();
            lenaId = lena.Id;
        }
        await RegisterAsync("dani", seed.Collect, comment: "Bringe Kisten mit");
        await RegisterAsync("dani", seed.Drive);
        await RegisterAsync("dani", seed.Sale, familyMemberId: lenaId);
        await RegisterAsync("dani", seed.NextDay);  // übermorgen: nicht in dieser Mail

        var service = CreateService();
        Assert.Equal(1, await service.SendDueRemindersAsync(EveAfter));
        Assert.Equal(0, await service.SendDueRemindersAsync(EveAfter.AddMinutes(15))); // nichts doppelt

        var (to, mail) = Assert.Single((await _mailQueue.RunAllAsync()).Reminders);
        Assert.Equal("dani@example.com", to);
        Assert.Equal(EventDay, mail.Date);
        Assert.Equal("Flomi 2027", mail.EventName);
        Assert.Equal("https://pfadiflomi.ch/user/dashboard", mail.DashboardUrl);
        Assert.Equal(new[] { "Fahrer", "Kleider", "Sammeln" }, mail.Items.Select(i => i.AreaName)); // nach Zeit sortiert

        var sale = mail.Items.Single(i => i.AreaName == "Kleider");
        Assert.Equal("Lena Muster", sale.ForPerson);
        Assert.True(sale.WithQrCode);
        var collect = mail.Items.Single(i => i.AreaName == "Sammeln");
        Assert.Equal("dani Muster", collect.ForPerson); // auch der Kontoinhaber steht mit Namen
        Assert.Equal("Ägerihalle", collect.Location);
        Assert.Equal("Bringe Kisten mit", collect.Comment);
    }

    [Fact]
    public async Task Skips_DisabledEvents_SampleEvents_AndPeopleWithoutNotifications()
    {
        var seed = await SeedAsync(remindersEnabled: false);
        await AddUsersAsync(MakeUser("dani"), MakeUser("ruhig", notifications: false));
        await RegisterAsync("dani", seed.Collect);
        Assert.Equal(0, await CreateService().SendDueRemindersAsync(EveAfter));

        using (var ctx = _db.CreateContext())
        {
            ctx.Events.Find(seed.Event.Id)!.RemindersEnabled = true;
            await ctx.SaveChangesAsync();
        }
        await RegisterAsync("ruhig", seed.Collect);

        Assert.Equal(1, await CreateService().SendDueRemindersAsync(EveAfter)); // nur dani

        var status = await CreateService().GetTomorrowStatusAsync(EveAfter);
        Assert.Equal(2, status.People);
        Assert.Equal(2, status.PeopleReminded); // «ruhig» gilt als erledigt, bekommt aber keine Mail
        Assert.True(status.SendTimePassed);
    }

    [Fact]
    public async Task SpontaneousOnly_ItemIsMarkedSpontaneous()
    {
        var seed = await SeedAsync();
        await AddUsersAsync(MakeUser("dani"));
        await RegisterAsync("dani", seed.Spontaneous);

        await CreateService().SendDueRemindersAsync(EveAfter);

        var (_, mail) = Assert.Single((await _mailQueue.RunAllAsync()).Reminders);
        Assert.True(Assert.Single(mail.Items).IsSpontaneous);
    }

    [Fact]
    public async Task Driver_SeesVehicleAndHelper()
    {
        var seed = await SeedAsync();
        await AddUsersAsync(MakeUser("dani"), MakeUser("marc"));
        await RegisterAsync("dani", seed.Drive);
        using (var ctx = _db.CreateContext())
        {
            var vehicle = new Vehicle { OwnerName = "Huber" };
            ctx.Vehicles.Add(vehicle);
            await ctx.SaveChangesAsync();
            ctx.VehicleAssignments.Add(new VehicleAssignment
            {
                VehicleId = vehicle.Id, EventId = seed.Event.Id,
                AssignedDates = { new AssignmentDate { Date = EventDay, DriverUserId = "dani", HelperUserId = "marc" } },
            });
            await ctx.SaveChangesAsync();
        }

        await CreateService().SendDueRemindersAsync(EveAfter);

        var (_, mail) = Assert.Single((await _mailQueue.RunAllAsync()).Reminders);
        Assert.Equal("Fahrzeug von Huber · Beifahrer: marc Muster", Assert.Single(mail.Items).VehicleInfo);
    }

    [Fact]
    public async Task TestReminder_UsesExampleArea_WhenNoOwnRegistrations()
    {
        var seed = await SeedAsync();
        await AddUsersAsync(MakeUser("admin"));

        Assert.True(await CreateService().SendTestReminderAsync("admin", seed.Event.Id));

        var (to, mail) = Assert.Single((await _mailQueue.RunAllAsync()).Reminders);
        Assert.Equal("admin@example.com", to);
        Assert.True(mail.IsTest);
        Assert.Single(mail.Items);
        Assert.False(mail.Items[0].WithQrCode); // Beispiel ohne echte Anmeldung: kein QR-Code
    }

    [Fact]
    public void Schedule_SendMomentIsSixPmTheEveningBefore()
    {
        Assert.Equal(new DateTime(2027, 4, 9, 18, 0, 0), ReminderSchedule.SendMomentFor(EventDay));
        Assert.False(ReminderSchedule.IsPastSendMoment(EventDay, EveBefore));
        Assert.True(ReminderSchedule.IsPastSendMoment(EventDay, EveAfter));
        Assert.Null(ReminderSchedule.DueAreaDate(EveBefore));
        Assert.Equal(EventDay, ReminderSchedule.DueAreaDate(EveAfter));
    }

    [Fact]
    public void Mail_ListsItems_EncodesInput_AndMarksTest()
    {
        var mail = new ReminderMail("Dani", EventDay, "Flomi 2027", new[]
        {
            new ReminderItem(1, "Fahrer", "Fahrer", "08:00-12:00", false, "Werkhof", null, null, "Fahrzeug von Huber · Beifahrer: Marc", false),
            new ReminderItem(2, "Sammeln", "Sammeln", "13:00-17:00", false, "Ägerihalle", "Lena Muster", "<b>Kisten</b>", null, false),
        }, "https://pfadiflomi.ch/user/dashboard", IsTest: true);

        var (subject, raw) = MailService.BuildDayBeforeReminder(mail);
        var body = System.Net.WebUtility.HtmlDecode(raw);

        Assert.Equal("[Test] ⏰ Morgen: 2 Einsätze · Flomi 2027", subject);
        Assert.Contains("Samstag 10.04.", body);
        Assert.Contains("Fahrzeug von Huber · Beifahrer: Marc", body);
        Assert.Contains("(Lena Muster)", body);
        Assert.Contains("&lt;b&gt;Kisten", raw);
        Assert.Contains("href='https://pfadiflomi.ch/user/dashboard'", raw);
    }
}
