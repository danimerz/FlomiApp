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

    [Fact]
    public void Display_IsSet() =>
        Assert.False(string.IsNullOrWhiteSpace(AppVersion.Display));
}
