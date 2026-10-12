using FlomiApp.Helpers;
using Xunit;

namespace FlomiApp.Tests.Helpers;

public class TimeHelperTests
{
    [Theory]
    [InlineData("730",   "07:30")]
    [InlineData("0730",  "07:30")]
    [InlineData("7:30",  "07:30")]
    [InlineData("7.30",  "07:30")]
    [InlineData("16:45", "16:45")]
    public void FormatTime_NormalizesValidTimes(string input, string expected)
    {
        Assert.Equal(expected, TimeHelper.FormatTime(input));
    }

    [Theory]
    [InlineData("2599")] // ungültige Stunde/Minute
    [InlineData("abc")]
    public void FormatTime_ReturnsInputUnchanged_WhenNotParsable(string input)
    {
        Assert.Equal(input, TimeHelper.FormatTime(input));
    }

    [Theory]
    [InlineData("0730-1630",       "07:30-16:30")]
    [InlineData("7:30-16:30",      "07:30-16:30")]
    [InlineData("07:30 - 16:30",   "07:30-16:30")]
    public void FormatTimeSlot_NormalizesValidRanges(string input, string expected)
    {
        Assert.Equal(expected, TimeHelper.FormatTimeSlot(input));
    }

    [Fact]
    public void FormatTimeSlot_FallsBackToFormatTime_ForSingleValue()
    {
        Assert.Equal("07:30", TimeHelper.FormatTimeSlot("730"));
    }
}
