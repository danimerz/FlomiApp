using FlomiApp.Data.Models;
using FlomiApp.Services;
using Xunit;

namespace FlomiApp.Tests.Services;

public class AdminMailContentTests
{
    private static AdminRegistrationMail Registration(bool cancelled = false, string? comment = "Bringe <b>Kisten</b> mit") => new(
        Cancelled: cancelled, PersonName: "Lena Muster", PersonStufe: "Wölfe", PersonBirthday: new DateTime(2016, 3, 2),
        ForFamilyMember: true, AccountName: "Eva Muster", AccountEmail: "eva@example.com", AccountPhone: "079 123 45 67",
        EventName: "Flomi Herbst", AreaName: "Geschirr", CategoryName: "Sammeln", Date: new DateTime(2026, 11, 20),
        TimeSlot: "08:00-12:00", Comment: comment, Registered: 3, Capacity: 4, AccountRegistrationsInEvent: 2,
        AtUtc: new DateTime(2026, 10, 31, 9, 15, 0, DateTimeKind.Utc), AdminUrl: "https://pfadiflomi.ch/admin/appointments");

    [Fact]
    public void Registration_ShowsDetails_AndEncodesUserInput()
    {
        var (subject, raw) = MailService.BuildAdminRegistration(Registration());
        var body = System.Net.WebUtility.HtmlDecode(raw); // Umlaute sind als &#228; kodiert

        Assert.Equal("📝 Neue Anmeldung – Geschirr · Lena Muster", subject);
        Assert.Contains("Bringe &lt;b&gt;Kisten&lt;/b&gt; mit", raw);
        Assert.DoesNotContain("<b>Kisten", raw);
        Assert.Contains("3 von 4 Plätzen (1 frei)", body);
        Assert.Contains("Eva Muster (meldet Familienmitglied an)", body);
        Assert.Contains("Geschirr (Sammeln)", body);
        Assert.Contains("Freitag, 20. November 2026", body);
        Assert.Contains("31.10.2026, 10:15 Uhr", body); // Schweizer Zeit (UTC+1 im Winter)
        Assert.Contains("href='https://pfadiflomi.ch/admin/appointments'", body);
    }

    [Fact]
    public void Cancellation_HasOwnSubject_AndLeavesOutComment()
    {
        var (subject, body) = MailService.BuildAdminRegistration(Registration(cancelled: true));

        Assert.Equal("❌ Stornierung – Geschirr · Lena Muster", subject);
        Assert.DoesNotContain("Kisten", body);
        Assert.Contains("Anmeldung storniert", body);
    }

    [Fact]
    public void NewUser_ShowsMethodAndConfirmation_AndSkipsEmptyRows()
    {
        var (subject, raw) = MailService.BuildAdminNewUser(new AdminNewUserMail(
            FullName: "Max Muster", Pfadiname: null, Stufe: "Pfadis", Birthday: null, Email: "max@example.com",
            EmailConfirmed: false, PhoneNumber: "079 000 00 00", Method: "E-Mail und Passwort",
            CreatedAtUtc: new DateTime(2026, 10, 31, 9, 0, 0, DateTimeKind.Utc), TotalUsers: 42, AdminUrl: null));

        var body = System.Net.WebUtility.HtmlDecode(raw);
        Assert.Equal("👤 Neues Konto – Max Muster", subject);
        Assert.Contains("max@example.com (noch nicht bestätigt)", body);
        Assert.Contains("E-Mail und Passwort", body);
        Assert.DoesNotContain("Pfadiname", body);
        Assert.DoesNotContain("Geburtstag", body);
        Assert.DoesNotContain("<a href", body);
    }
}
