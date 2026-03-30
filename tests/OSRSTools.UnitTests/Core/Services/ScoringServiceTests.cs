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
            TotalProfitBreakpoints = new List<BreakpointEntry>
            {
                new() { Threshold = 10000, Score = 0.1 },
                new() { Threshold = 50000, Score = 0.3 },
                new() { Threshold = 200000, Score = 0.6 },
                new() { Threshold = 500000, Score = 0.8 },
                new() { Threshold = 2000000, Score = 1.0 }
            },
            RoiBreakpoints = new List<BreakpointEntry>
            {
                new() { Threshold = 0.0, Score = 0.0 },
                new() { Threshold = 1.0, Score = 0.05 },
                new() { Threshold = 3.0, Score = 0.30 },
                new() { Threshold = 5.0, Score = 0.60 },
                new() { Threshold = 10.0, Score = 0.85 },
                new() { Threshold = 20.0, Score = 1.0 }
            },
            VolumeWeight = 0.20,
            TotalProfitWeight = 0.35,
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

    [Fact]
    public void ScoreVolume_ZeroVolume_ReturnsMinBreakpointScore()
    {
        var result = _sut.ScoreVolume(0);
        Assert.Equal(0.1, result);
    }

    #endregion

    #region ScoreTotalProfit — Breakpoint Interpolation

    [Fact]
    public void ScoreTotalProfit_AtExactBreakpoint_ReturnsExactScore()
    {
        var result = _sut.ScoreTotalProfit(50000);
        Assert.Equal(0.3, result);
    }

    [Fact]
    public void ScoreTotalProfit_BelowMinimum_ReturnsMinScore()
    {
        var result = _sut.ScoreTotalProfit(5000);
        Assert.Equal(0.1, result);
    }

    [Fact]
    public void ScoreTotalProfit_AboveMaximum_ReturnsMaxScore()
    {
        var result = _sut.ScoreTotalProfit(5_000_000);
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void ScoreTotalProfit_BetweenBreakpoints_InterpolatesLinearly()
    {
        // 125000 between 50000 (0.3) and 200000 (0.6)
        // Progress: (125000 - 50000) / (200000 - 50000) = 0.5
        // Score: 0.3 + 0.5 * 0.3 = 0.45
        var result = _sut.ScoreTotalProfit(125000);
        Assert.Equal(0.45, result, precision: 2);
    }

    #endregion

    #region ScoreRoi — Updated Breakpoints

    [Fact]
    public void ScoreRoi_AtZero_ReturnsZero()
    {
        var result = _sut.ScoreRoi(0.0);
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void ScoreRoi_AtTwoPercent_ReturnsLowScore()
    {
        // 2.0 between 1.0 (0.05) and 3.0 (0.30)
        // Progress: (2.0 - 1.0) / (3.0 - 1.0) = 0.5
        // Score: 0.05 + 0.5 * 0.25 = 0.175
        var result = _sut.ScoreRoi(2.0);
        Assert.Equal(0.175, result, precision: 3);
    }

    [Fact]
    public void ScoreRoi_AboveMaximum_ReturnsMaxScore()
    {
        var result = _sut.ScoreRoi(50.0);
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void ScoreRoi_BelowMinimum_ReturnsZero()
    {
        var result = _sut.ScoreRoi(-5.0);
        Assert.Equal(0.0, result);
    }

    #endregion

    #region CalculateConfidence — New Formula with Volatility

    [Fact]
    public void CalculateConfidence_HighDataLowVolatility_ReturnsHighScore()
    {
        // volume: min(50000/50000, 1) * 0.40 = 0.40
        // stability: volatility <= 5 → 1.0 * 0.35 = 0.35
        // windows: min(3/3, 1) * 0.25 = 0.25
        // total: 1.0
        var result = _sut.CalculateConfidence(3, 50000, 3.0);
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateConfidence_LowWindowsAndVolume_ReturnsLowScore()
    {
        // volume: min(10000/50000, 1) * 0.40 = 0.08
        // stability: volatility <= 5 → 1.0 * 0.35 = 0.35
        // windows: min(1/3, 1) * 0.25 = 0.0833
        var result = _sut.CalculateConfidence(1, 10000, 2.0);
        Assert.Equal(0.51, result);
    }

    [Fact]
    public void CalculateConfidence_HighVolatility_ReducesConfidence()
    {
        // volume: min(50000/50000, 1) * 0.40 = 0.40
        // stability: volatility > 30 → 0.1 * 0.35 = 0.035
        // windows: min(3/3, 1) * 0.25 = 0.25
        // total: 0.685 → rounded to 0.69 or 0.68 depending on floating point
        var result = _sut.CalculateConfidence(3, 50000, 50.0);
        Assert.Equal(0.68, result, precision: 2);
    }

    [Fact]
    public void CalculateConfidence_ModerateVolatility_GivesMiddleStabilityScore()
    {
        // volatility 10% → stability = 0.7
        // volume: min(50000/50000, 1) * 0.40 = 0.40
        // stability: 0.7 * 0.35 = 0.245
        // windows: min(3/3, 1) * 0.25 = 0.25
        // total: 0.895 → 0.9
        var result = _sut.CalculateConfidence(3, 50000, 10.0);
        Assert.Equal(0.9, result);
    }

    [Fact]
    public void CalculateConfidence_ZeroEverything_ReturnsStabilityOnly()
    {
        // volume: 0
        // stability: volatility 0 → 1.0 * 0.35 = 0.35
        // windows: 0
        // total: 0.35
        var result = _sut.CalculateConfidence(0, 0, 0);
        Assert.Equal(0.35, result);
    }

    #endregion

    #region CalculateFlipScore

    [Fact]
    public void CalculateFlipScore_HighQualityCandidate_ReturnsHighScore()
    {
        var candidate = new FlipCandidate
        {
            Volume24Hr = 100000,
            ProfitPerCycle = 2_000_000,
            TotalProfit = 500_000,
            RoiPercent = 8.0,
            GpPerHour = 500000,
            BuyWindowsUsed = 4,
            SellWindowsUsed = 4,
            PriceVolatilityPercent = 3.0,
            ProfitPerUnit = 100
        };

        var result = _sut.CalculateFlipScore(candidate);

        Assert.True(result > 0);
        Assert.True(result <= 10.0);
    }

    [Fact]
    public void CalculateFlipScore_ZeroValues_ReturnsLowScore()
    {
        var candidate = new FlipCandidate
        {
            Volume24Hr = 0,
            ProfitPerCycle = 0,
            RoiPercent = 0,
            GpPerHour = 0,
            BuyWindowsUsed = 0,
            SellWindowsUsed = 0,
            PriceVolatilityPercent = 0
        };

        var result = _sut.CalculateFlipScore(candidate);
        Assert.True(result >= 0);
        Assert.True(result < 2.0);
    }

    [Fact]
    public void CalculateFlipScore_HighVolatility_ReducesScore()
    {
        var stableCandidate = new FlipCandidate
        {
            Volume24Hr = 100000, ProfitPerCycle = 1_000_000, TotalProfit = 200_000, RoiPercent = 5.0, GpPerHour = 500000,
            BuyWindowsUsed = 3, SellWindowsUsed = 3, PriceVolatilityPercent = 2.0, ProfitPerUnit = 100
        };
        var volatileCandidate = new FlipCandidate
        {
            Volume24Hr = 100000, ProfitPerCycle = 1_000_000, TotalProfit = 200_000, RoiPercent = 5.0, GpPerHour = 500000,
            BuyWindowsUsed = 3, SellWindowsUsed = 3, PriceVolatilityPercent = 40.0, ProfitPerUnit = 100
        };

        var stableScore = _sut.CalculateFlipScore(stableCandidate);
        var volatileScore = _sut.CalculateFlipScore(volatileCandidate);

        Assert.True(stableScore > volatileScore);
    }

    #endregion
}
