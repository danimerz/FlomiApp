using FlomiApp.Helpers;
using Xunit;

namespace FlomiApp.Tests.Helpers;

public class PhoneHelperTests
{
    [Theory]
    [InlineData("0791234567",     "+41 79 123 45 67")]
    [InlineData("+41791234567",   "+41 79 123 45 67")]
    [InlineData("0041 79 123 45 67", "+41 79 123 45 67")]
    [InlineData("079 123 45 67",  "+41 79 123 45 67")]
    public void FormatSwissPhone_NormalizesValidNumbers(string input, string expected)
    {
        Assert.Equal(expected, PhoneHelper.FormatSwissPhone(input));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("not a phone number")]
    public void FormatSwissPhone_ReturnsInputUnchanged_WhenNotRecognizable(string input)
    {
        Assert.Equal(input, PhoneHelper.FormatSwissPhone(input));
    }

    [Fact]
    public void FormatSwissPhone_ReturnsNull_ForNullInput()
    {
        Assert.Null(PhoneHelper.FormatSwissPhone(null));
    }
}
