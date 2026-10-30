using FlomiApp.Helpers;

namespace FlomiApp.Tests.Helpers;

public class VehiclePlanDaysTests
{
    private static readonly DateTime EventDate = new(2027, 4, 17);

    [Fact]
    public void WithoutPickupRange_ShowsEightDaysBeforeEvent()
    {
        var days = VehiclePlanDays.For(EventDate, null, null);

        Assert.Equal(new DateTime(2027, 4, 9), days.First());
        Assert.Equal(EventDate, days.Last());
        Assert.Equal(9, days.Count);
    }

    [Fact]
    public void UsesPickupRange()
    {
        var days = VehiclePlanDays.For(EventDate, new DateTime(2027, 4, 12), new DateTime(2027, 4, 15));

        Assert.Equal(new DateTime(2027, 4, 12), days.First());
        Assert.Equal(new DateTime(2027, 4, 15), days.Last());
    }

    [Fact]
    public void PickupFromOnly_RunsUntilEventDay()
    {
        var days = VehiclePlanDays.For(EventDate, new DateTime(2027, 4, 5), null);

        Assert.Equal(new DateTime(2027, 4, 5), days.First());
        Assert.Equal(EventDate, days.Last());
    }

    [Fact]
    public void ExtendsToDaysAlreadyInUse()
    {
        var days = VehiclePlanDays.For(EventDate, new DateTime(2027, 4, 12), new DateTime(2027, 4, 15),
                                       new[] { new DateTime(2027, 4, 10, 9, 0, 0), new DateTime(2027, 4, 16) });

        Assert.Equal(new DateTime(2027, 4, 10), days.First());
        Assert.Equal(new DateTime(2027, 4, 16), days.Last());
    }

    [Fact]
    public void IsCapped()
    {
        var days = VehiclePlanDays.For(EventDate, new DateTime(2026, 1, 1), new DateTime(2027, 4, 17));
        Assert.Equal(VehiclePlanDays.MaxDays, days.Count);
    }
}
