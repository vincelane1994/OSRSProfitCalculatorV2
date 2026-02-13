using OSRSTools.Core.Configuration;
using Xunit;

namespace OSRSTools.UnitTests.Core.Configuration;

public class BreakpointEntryTests
{
    [Fact]
    public void DefaultValues_Threshold_EqualsZero()
    {
        var entry = new BreakpointEntry();

        Assert.Equal(0.0, entry.Threshold);
    }

    [Fact]
    public void DefaultValues_Score_EqualsZero()
    {
        var entry = new BreakpointEntry();

        Assert.Equal(0.0, entry.Score);
    }

    [Fact]
    public void SetProperties_Threshold_UpdatesCorrectly()
    {
        var entry = new BreakpointEntry { Threshold = 10_000.0 };

        Assert.Equal(10_000.0, entry.Threshold);
    }

    [Fact]
    public void SetProperties_Score_UpdatesCorrectly()
    {
        var entry = new BreakpointEntry { Score = 0.75 };

        Assert.Equal(0.75, entry.Score);
    }

    [Fact]
    public void SetProperties_BothProperties_UpdateCorrectly()
    {
        var entry = new BreakpointEntry
        {
            Threshold = 50_000.0,
            Score = 0.60
        };

        Assert.Equal(50_000.0, entry.Threshold);
        Assert.Equal(0.60, entry.Score);
    }
}
