using OSRSTools.Core.Configuration;
using Xunit;

namespace OSRSTools.UnitTests.Core.Configuration;

public class ScoringConfigurationTests
{
    [Fact]
    public void DefaultValues_VolumeBreakpoints_IsEmptyList()
    {
        var config = new ScoringConfiguration();

        Assert.NotNull(config.VolumeBreakpoints);
        Assert.Empty(config.VolumeBreakpoints);
    }

    [Fact]
    public void DefaultValues_TotalProfitBreakpoints_IsEmptyList()
    {
        var config = new ScoringConfiguration();

        Assert.NotNull(config.TotalProfitBreakpoints);
        Assert.Empty(config.TotalProfitBreakpoints);
    }

    [Fact]
    public void DefaultValues_RoiBreakpoints_IsEmptyList()
    {
        var config = new ScoringConfiguration();

        Assert.NotNull(config.RoiBreakpoints);
        Assert.Empty(config.RoiBreakpoints);
    }

    [Fact]
    public void DefaultValues_VolumeWeight_Equals0Point20()
    {
        var config = new ScoringConfiguration();

        Assert.Equal(0.20, config.VolumeWeight);
    }

    [Fact]
    public void DefaultValues_TotalProfitWeight_Equals0Point35()
    {
        var config = new ScoringConfiguration();

        Assert.Equal(0.35, config.TotalProfitWeight);
    }

    [Fact]
    public void DefaultValues_RoiWeight_Equals0Point20()
    {
        var config = new ScoringConfiguration();

        Assert.Equal(0.20, config.RoiWeight);
    }

    [Fact]
    public void DefaultValues_GpPerHourWeight_Equals0Point25()
    {
        var config = new ScoringConfiguration();

        Assert.Equal(0.25, config.GpPerHourWeight);
    }

    [Fact]
    public void DefaultValues_AllWeights_SumToOne()
    {
        var config = new ScoringConfiguration();

        var total = config.VolumeWeight + config.TotalProfitWeight
                  + config.RoiWeight + config.GpPerHourWeight;

        Assert.Equal(1.0, total, precision: 10);
    }

    [Fact]
    public void DefaultValues_MinWindowsForHighConfidence_Equals3()
    {
        var config = new ScoringConfiguration();

        Assert.Equal(3, config.MinWindowsForHighConfidence);
    }

    [Fact]
    public void DefaultValues_MinVolumeForHighConfidence_Equals50000()
    {
        var config = new ScoringConfiguration();

        Assert.Equal(50_000, config.MinVolumeForHighConfidence);
    }

    [Fact]
    public void SetProperties_VolumeBreakpoints_StoresEntries()
    {
        var config = new ScoringConfiguration
        {
            VolumeBreakpoints =
            [
                new() { Threshold = 1_000, Score = 0.2 },
                new() { Threshold = 10_000, Score = 0.5 },
                new() { Threshold = 100_000, Score = 1.0 }
            ]
        };

        Assert.Equal(3, config.VolumeBreakpoints.Count);
        Assert.Equal(1_000, config.VolumeBreakpoints[0].Threshold);
        Assert.Equal(0.5, config.VolumeBreakpoints[1].Score);
        Assert.Equal(1.0, config.VolumeBreakpoints[2].Score);
    }
}
