using OSRSTools.Core.Entities;
using Xunit;

namespace OSRSTools.UnitTests.Core.Entities;

public class FlipSettingsTests
{
    [Fact]
    public void DefaultValues_MinVolume_Equals10000()
    {
        var settings = new FlipSettings();

        Assert.Equal(10_000, settings.MinVolume);
    }

    [Fact]
    public void DefaultValues_MinBuyLimit_Equals100()
    {
        var settings = new FlipSettings();

        Assert.Equal(100, settings.MinBuyLimit);
    }

    [Fact]
    public void DefaultValues_MinMargin_Equals10()
    {
        var settings = new FlipSettings();

        Assert.Equal(10, settings.MinMargin);
    }

    [Fact]
    public void DefaultValues_MaxInvestment_Equals10Million()
    {
        var settings = new FlipSettings();

        Assert.Equal(10_000_000L, settings.MaxInvestment);
    }

    [Fact]
    public void DefaultValues_BuyLimitCycleHours_Equals4()
    {
        var settings = new FlipSettings();

        Assert.Equal(4.0, settings.BuyLimitCycleHours);
    }

    [Fact]
    public void DefaultValues_MaxResults_Equals100()
    {
        var settings = new FlipSettings();

        Assert.Equal(100, settings.MaxResults);
    }

    [Fact]
    public void SetProperties_AllProperties_UpdateCorrectly()
    {
        var settings = new FlipSettings
        {
            MinVolume = 50_000,
            MinBuyLimit = 500,
            MinMargin = 25,
            MaxInvestment = 100_000_000L,
            BuyLimitCycleHours = 8.0,
            MaxResults = 50
        };

        Assert.Equal(50_000, settings.MinVolume);
        Assert.Equal(500, settings.MinBuyLimit);
        Assert.Equal(25, settings.MinMargin);
        Assert.Equal(100_000_000L, settings.MaxInvestment);
        Assert.Equal(8.0, settings.BuyLimitCycleHours);
        Assert.Equal(50, settings.MaxResults);
    }

    [Fact]
    public void SetProperties_MaxInvestment_AcceptsLongValues()
    {
        var settings = new FlipSettings
        {
            MaxInvestment = 5_000_000_000L // 5B — exceeds int.MaxValue
        };

        Assert.Equal(5_000_000_000L, settings.MaxInvestment);
    }

    [Fact]
    public void SetProperties_NegativeMinVolume_AcceptsValue()
    {
        var settings = new FlipSettings { MinVolume = -1 };

        Assert.Equal(-1, settings.MinVolume);
    }

    [Fact]
    public void SetProperties_ZeroBuyLimitCycleHours_AcceptsValue()
    {
        var settings = new FlipSettings { BuyLimitCycleHours = 0.0 };

        Assert.Equal(0.0, settings.BuyLimitCycleHours);
    }
}
