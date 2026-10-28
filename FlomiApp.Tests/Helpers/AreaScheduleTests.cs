using FlomiApp.Data.Models;
using FlomiApp.Helpers;
using Xunit;

namespace FlomiApp.Tests.Helpers;

public class AreaScheduleTests
{
    private static Area MakeArea(bool spontaneous = false) => new()
    {
        Date                   = new DateTime(2026, 11, 20),
        TimeSlot               = "08:00-17:00",
        AlternativeTimeSlot    = "08:00-12:00",
        AlternativeMaxCapacity = 3,
        IsSpontaneous          = spontaneous
    };

    [Fact]
    public void TimeSlot_ReturnsMainOrAlternativeSlot()
    {
        var area = MakeArea();
        Assert.Equal("08:00-17:00", AreaSchedule.TimeSlot(area));
        Assert.Equal("08:00-12:00", AreaSchedule.TimeSlot(area, useAlternativeSlot: true));
    }

    [Fact]
    public void TimeSlot_Spontaneous_ReturnsSpontaneousText()
    {
        Assert.Equal(AreaSchedule.SpontaneousTime, AreaSchedule.TimeSlot(MakeArea(spontaneous: true), true));
    }

    [Fact]
    public void Date_Spontaneous_IsNull()
    {
        Assert.Null(AreaSchedule.Date(MakeArea(spontaneous: true)));
        Assert.Equal(new DateTime(2026, 11, 20), AreaSchedule.Date(MakeArea()));
    }

    [Fact]
    public void FormatDate_UsesSwissFormat_OrSpontaneousText()
    {
        Assert.Equal("20.11.2026", AreaSchedule.FormatDate(new DateTime(2026, 11, 20), "dd.MM.yyyy"));
        Assert.Equal("Freitag, 20.11.2026", AreaSchedule.FormatDate(new DateTime(2026, 11, 20), "dddd, dd.MM.yyyy"));
        Assert.Equal(AreaSchedule.SpontaneousDate, AreaSchedule.FormatDate(null, "dd.MM.yyyy"));
    }
}
