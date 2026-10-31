using FlomiApp.Helpers;
using FlomiApp.Services;
using Xunit;

namespace FlomiApp.Tests.Helpers;

public class RegistrationMessagesTests
{
    [Fact]
    public void TimeConflict_ForSelf()
    {
        var check = new RegistrationCheck(RegistrationBlock.TimeConflict,
            OtherAreaName: "Fahrer", OtherDate: new DateTime(2027, 4, 10), OtherTimeSlot: "08:00-12:00");

        Assert.Equal("Du bist zu dieser Zeit schon für «Fahrer» eingetragen (Sa 10.04., 08:00-12:00). " +
                     "Beides gleichzeitig geht nicht.", RegistrationMessages.Describe(check));
        Assert.True(RegistrationMessages.CanSwitch(check));
    }

    [Fact]
    public void AlreadyRegistered_ForFamilyMember()
    {
        var check = new RegistrationCheck(RegistrationBlock.AlreadyRegistered);

        Assert.Equal("✓ Lena ist hier bereits angemeldet.", RegistrationMessages.Describe(check, "Lena"));
        Assert.False(RegistrationMessages.CanSwitch(check));
    }

    [Theory]
    [InlineData(null,   "Ab 16 Jahren – du bist 14.")]
    [InlineData("Lena", "Ab 16 Jahren – Lena ist 14.")]
    public void MinAge_ShowsAge(string? name, string expected) =>
        Assert.Equal(expected, RegistrationMessages.Describe(
            new RegistrationCheck(RegistrationBlock.MinAge, MinAge: 16, PersonAge: 14), name));

    [Fact]
    public void SaleAndCake_NameTheOtherArea()
    {
        Assert.Equal("Du hast bereits den Verkaufsbereich «Kleider». Pro Event ist nur einer möglich.",
            RegistrationMessages.Describe(new RegistrationCheck(RegistrationBlock.SaleLimit, OtherAreaName: "Kleider")));
        Assert.Equal("Lena bringt an diesem Tag schon einen Kuchen («Kuchen Samstag»).",
            RegistrationMessages.Describe(new RegistrationCheck(RegistrationBlock.CakeLimit, OtherAreaName: "Kuchen Samstag"), "Lena"));
    }
}
