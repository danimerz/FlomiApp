using FlomiApp.Helpers;

namespace FlomiApp.Tests.Helpers;

public class AppVersionTests
{
    [Theory]
    [InlineData("1.0.42+3f2a9c1d0e5b7a8c", "1.0.42 (3f2a9c1)")]
    [InlineData("1.0.42+abc",              "1.0.42 (abc)")]
    [InlineData("1.0.0",                   "1.0.0")]
    [InlineData(null,                      "unbekannt")]
    public void Format_ShortensCommit(string? input, string expected) =>
        Assert.Equal(expected, AppVersion.Format(input));

    [Theory]
    [InlineData("2026-10-30T13:35:00.0000000Z", "1.0.42 (3f2a9c1) · 30.10.2026 14:35")] // Winterzeit
    [InlineData("2027-04-10T07:05:00Z",         "1.0.42 (3f2a9c1) · 10.04.2027 09:05")] // Sommerzeit
    [InlineData("kein Datum",                   "1.0.42 (3f2a9c1)")]
    public void Format_AppendsBuildTimeInSwissTime(string buildTime, string expected) =>
        Assert.Equal(expected, AppVersion.Format("1.0.42+3f2a9c1d0e5b", buildTime));

    [Fact]
    public void Display_IsSet() =>
        Assert.False(string.IsNullOrWhiteSpace(AppVersion.Display));
}
