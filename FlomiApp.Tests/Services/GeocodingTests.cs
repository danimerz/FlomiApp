using FlomiApp.Services;
using Xunit;

namespace FlomiApp.Tests.Services;

public class GeocodingTests
{
    [Fact]
    public void PickBest_SkipsSameStreetInOtherPostalCode()
    {
        // geo.admin.ch sucht unscharf: «Zugerstrasse 10» gibt es auch in Küssnacht
        var result = GeoAdminGeocodingService.PickBest("Zugerstrasse 10, 6314 Unterägeri", new[]
        {
            ("Zugerstrasse 10 6403 Küssnacht am Rigi", 47.09, 8.44),
            ("Zugerstrasse 10 6314 Unterägeri",        47.138, 8.582),
        });

        Assert.NotNull(result);
        Assert.Equal(47.138, result!.Point.Latitude);
        Assert.False(result.Approximate);
    }

    [Fact]
    public void PickBest_MarksDifferentHouseNumberAsApproximate()
    {
        var result = GeoAdminGeocodingService.PickBest("Wilbrunnenstrasse 5, 6314 Unterägeri", new[]
        {
            ("Wilbrunnenstrasse 25 6314 Unterägeri", 47.13, 8.59),
        });

        Assert.NotNull(result);
        Assert.True(result!.Approximate);
    }

    [Fact]
    public void PickBest_AcceptsAbbreviatedStreet()
    {
        var result = GeoAdminGeocodingService.PickBest("Zugerstr. 10, 6314 Unterägeri", new[]
        {
            ("Zugerstrasse 10 6314 Unterägeri", 47.138, 8.582),
        });

        Assert.False(result!.Approximate);
    }

    [Fact]
    public void PickBest_ReturnsNullWhenNoCandidateHasThePostalCode()
    {
        var result = GeoAdminGeocodingService.PickBest("Phantasieweg 99, 6314 Unterägeri", new[]
        {
            ("Phantasieweg 9 8000 Zürich", 47.37, 8.54),
        });

        Assert.Null(result);
    }
}
