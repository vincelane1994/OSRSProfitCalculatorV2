using Microsoft.Extensions.Options;
using OSRSTools.Core.Configuration;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Services;
using Xunit;

namespace OSRSTools.UnitTests.Core.Services;

public class ScoringServiceTests
{
    private readonly ScoringService _sut;

    public ScoringServiceTests()
    {
        var config = Options.Create(new ScoringConfiguration
        {
            VolumeBreakpoints = new List<BreakpointEntry>
            {
                new() { Threshold = 1000, Score = 0.1 },
                new() { Threshold = 10000, Score = 0.3 },
                new() { Threshold = 50000, Score = 0.6 },
                new() { Threshold = 200000, Score = 1.0 }
            },
            MarginBreakpoints = new List<BreakpointEntry>
            {
                new() { Threshold = 5, Score = 0.05 },
                new() { Threshold = 50, Score = 0.2 },
                new() { Threshold = 200, Score = 0.5 },
                new() { Threshold = 1000, Score = 0.8 },
                new() { Threshold = 5000, Score = 1.0 }
            },
            RoiBreakpoints = new List<BreakpointEntry>
            {
                new() { Threshold = 0.5, Score = 0.1 },
                new() { Threshold = 2.0, Score = 0.3 },
                new() { Threshold = 5.0, Score = 0.6 },
                new() { Threshold = 15.0, Score = 1.0 }
            },
            VolumeWeight = 0.30,
            MarginWeight = 0.25,
            RoiWeight = 0.20,
            GpPerHourWeight = 0.25,
            MinWindowsForHighConfidence = 3,
            MinVolumeForHighConfidence = 50000
        });
        _sut = new ScoringService(config);
    }

    #region ScoreVolume — Breakpoint Interpolation

    [Fact]
    public void ScoreVolume_AtExactBreakpoint_ReturnsExactScore()
    {
        var result = _sut.ScoreVolume(10000);
        Assert.Equal(0.3, result);
    }

    [Fact]
    public void ScoreVolume_BetweenBreakpoints_InterpolatesLinearly()
    {
        // 5500 between 1000 (0.1) and 10000 (0.3)
        // Progress: (5500 - 1000) / (10000 - 1000) = 0.5
        // Score: 0.1 + 0.5 * 0.2 = 0.2
        var result = _sut.ScoreVolume(5500);
        Assert.Equal(0.2, result, precision: 2);
    }

    [Fact]
    public void ScoreVolume_BelowMinimumBreakpoint_ReturnsMinScore()
    {
        var result = _sut.ScoreVolume(500);
        Assert.Equal(0.1, result);
    }

    [Fact]
    public void ScoreVolume_AboveMaximumBreakpoint_ReturnsMaxScore()
    {
        var result = _sut.ScoreVolume(500000);
        Assert.Equal(1.0, result);
    }

    #endregion

    #region ScoreMargin — Breakpoint Interpolation

    [Fact]
    public void ScoreMargin_AtExactBreakpoint_ReturnsExactScore()
    {
        var result = _sut.ScoreMargin(200);
        Assert.Equal(0.5, result);
    }

    [Fact]
    public void ScoreMargin_BetweenBreakpoints_InterpolatesLinearly()
    {
        // 125 between 50 (0.2) and 200 (0.5)
        // Progress: (125 - 50) / (200 - 50) = 0.5
        // Score: 0.2 + 0.5 * 0.3 = 0.35
        var result = _sut.ScoreMargin(125);
        Assert.Equal(0.35, result, precision: 2);
    }

    #endregion

    #region ScoreRoi — Breakpoint Interpolation

    [Fact]
    public void ScoreRoi_BelowMinimum_ReturnsMinScore()
    {
        var result = _sut.ScoreRoi(0.1);
        Assert.Equal(0.1, result);
    }

    [Fact]
    public void ScoreRoi_AboveMaximum_ReturnsMaxScore()
    {
        var result = _sut.ScoreRoi(50.0);
        Assert.Equal(1.0, result);
    }

    #endregion

    #region CalculateConfidence

    [Fact]
    public void CalculateConfidence_MaxWindowsAndVolume_ReturnsOne()
    {
        // windows: min(3/3, 1) * 0.6 = 0.6
        // volume: min(50000/50000, 1) * 0.4 = 0.4
        // total: 1.0
        var result = _sut.CalculateConfidence(3, 50000);
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateConfidence_LowWindowsAndVolume_ReturnsLowScore()
    {
        // windows: min(1/3, 1) * 0.6 = 0.2
        // volume: min(10000/50000, 1) * 0.4 = 0.08
        // total: 0.28
        var result = _sut.CalculateConfidence(1, 10000);
        Assert.Equal(0.28, result);
    }

    [Fact]
    public void CalculateConfidence_ZeroWindowsAndVolume_ReturnsZero()
    {
        var result = _sut.CalculateConfidence(0, 0);
        Assert.Equal(0.0, result);
    }

    #endregion

    #region CalculateFlipScore

    [Fact]
    public void CalculateFlipScore_HighQualityCandidate_ReturnsHighScore()
    {
        var candidate = new FlipCandidate
        {
            Volume24Hr = 100000,
            Margin = 500,
            RoiPercent = 8.0,
            GpPerHour = 500000,
            HasSufficientData = true
        };

        var result = _sut.CalculateFlipScore(candidate);

        Assert.True(result > 0);
        Assert.True(result <= 10.0);
    }

    [Fact]
    public void CalculateFlipScore_ZeroValues_ReturnsLowScore()
    {
        // Zero values still get minimum breakpoint scores (not zero),
        // but confidence is 0 for HasSufficientData=false with 0 volume,
        // yielding windowScore=min(2/3,1)*0.6=0.4, volumeScore=0 => confidence=0.4
        // Sub-scores use min breakpoint values, so result is small but non-zero.
        var candidate = new FlipCandidate
        {
            Volume24Hr = 0,
            Margin = 0,
            RoiPercent = 0,
            GpPerHour = 0,
            HasSufficientData = false
        };

        var result = _sut.CalculateFlipScore(candidate);
        Assert.True(result >= 0);
        Assert.True(result < 1.0);
    }

    #endregion
}
