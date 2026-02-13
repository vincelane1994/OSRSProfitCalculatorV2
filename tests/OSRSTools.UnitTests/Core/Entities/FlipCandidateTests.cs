using OSRSTools.Core.Entities;
using Xunit;

namespace OSRSTools.UnitTests.Core.Entities;

public class FlipCandidateTests
{
    [Fact]
    public void IsProfitable_PositiveProfit_ReturnsTrue()
    {
        var candidate = new FlipCandidate { ProfitPerUnit = 100 };

        Assert.True(candidate.IsProfitable);
    }

    [Fact]
    public void IsProfitable_ZeroProfit_ReturnsFalse()
    {
        var candidate = new FlipCandidate { ProfitPerUnit = 0 };

        Assert.False(candidate.IsProfitable);
    }

    [Fact]
    public void IsProfitable_NegativeProfit_ReturnsFalse()
    {
        var candidate = new FlipCandidate { ProfitPerUnit = -50 };

        Assert.False(candidate.IsProfitable);
    }

    [Fact]
    public void DefaultValues_NumericPropertiesAreZero()
    {
        var candidate = new FlipCandidate();

        Assert.Equal(0, candidate.ItemId);
        Assert.Equal(0, candidate.BuyLimit);
        Assert.Equal(0, candidate.RecommendedBuyPrice);
        Assert.Equal(0, candidate.RecommendedSellPrice);
        Assert.Equal(0, candidate.Margin);
        Assert.Equal(0, candidate.TaxAmount);
        Assert.Equal(0, candidate.ProfitPerUnit);
        Assert.Equal(0, candidate.Quantity);
        Assert.Equal(0L, candidate.TotalProfit);
        Assert.Equal(0.0, candidate.RoiPercent);
        Assert.Equal(0.0, candidate.GpPerHour);
        Assert.Equal(0.0, candidate.EstimatedFillHours);
        Assert.Equal(0, candidate.Volume24Hr);
        Assert.Equal(0.0, candidate.ConfidenceRating);
        Assert.Equal(0.0, candidate.FlipScore);
    }

    [Fact]
    public void DefaultValues_StringPropertiesAreEmpty()
    {
        var candidate = new FlipCandidate();

        Assert.Equal(string.Empty, candidate.Name);
    }

    [Fact]
    public void DefaultValues_BooleanPropertiesAreFalse()
    {
        var candidate = new FlipCandidate();

        Assert.False(candidate.Members);
        Assert.False(candidate.HasSufficientData);
        Assert.False(candidate.IsProfitable);
    }

    [Fact]
    public void PropertyInitializers_AllProperties_SetCorrectly()
    {
        var candidate = new FlipCandidate
        {
            ItemId = 2,
            Name = "Cannonball",
            Members = true,
            BuyLimit = 5000,
            RecommendedBuyPrice = 200,
            RecommendedSellPrice = 220,
            Margin = 20,
            TaxAmount = 4,
            ProfitPerUnit = 16,
            Quantity = 5000,
            TotalProfit = 80_000L,
            RoiPercent = 8.0,
            GpPerHour = 20_000.0,
            EstimatedFillHours = 4.0,
            Volume24Hr = 100_000,
            ConfidenceRating = 0.85,
            FlipScore = 0.72,
            HasSufficientData = true
        };

        Assert.Equal(2, candidate.ItemId);
        Assert.Equal("Cannonball", candidate.Name);
        Assert.True(candidate.Members);
        Assert.Equal(5000, candidate.BuyLimit);
        Assert.Equal(200, candidate.RecommendedBuyPrice);
        Assert.Equal(220, candidate.RecommendedSellPrice);
        Assert.Equal(20, candidate.Margin);
        Assert.Equal(4, candidate.TaxAmount);
        Assert.Equal(16, candidate.ProfitPerUnit);
        Assert.Equal(5000, candidate.Quantity);
        Assert.Equal(80_000L, candidate.TotalProfit);
        Assert.Equal(8.0, candidate.RoiPercent);
        Assert.Equal(20_000.0, candidate.GpPerHour);
        Assert.Equal(4.0, candidate.EstimatedFillHours);
        Assert.Equal(100_000, candidate.Volume24Hr);
        Assert.Equal(0.85, candidate.ConfidenceRating);
        Assert.Equal(0.72, candidate.FlipScore);
        Assert.True(candidate.HasSufficientData);
        Assert.True(candidate.IsProfitable);
    }

    [Fact]
    public void PropertyInitializers_PartialProperties_SetCorrectly()
    {
        var candidate = new FlipCandidate
        {
            ItemId = 10,
            Name = "Rune arrow",
            ProfitPerUnit = 5
        };

        Assert.Equal(10, candidate.ItemId);
        Assert.Equal("Rune arrow", candidate.Name);
        Assert.Equal(5, candidate.ProfitPerUnit);
        // Unset properties retain defaults
        Assert.Equal(0, candidate.BuyLimit);
        Assert.Equal(0.0, candidate.FlipScore);
        Assert.False(candidate.Members);
    }

    [Fact]
    public void TotalProfit_HighValueFlip_NoOverflow()
    {
        var candidate = new FlipCandidate
        {
            TotalProfit = 10_000_000_000L // 10B — exceeds int.MaxValue (~2.1B)
        };

        Assert.Equal(10_000_000_000L, candidate.TotalProfit);
    }

    [Fact]
    public void TotalProfit_MaxValueFlip_HandlesLongCorrectly()
    {
        var candidate = new FlipCandidate
        {
            TotalProfit = long.MaxValue
        };

        Assert.Equal(long.MaxValue, candidate.TotalProfit);
    }

    [Fact]
    public void ProfitPerUnit_Int32MinValue_HandlesCorrectly()
    {
        var candidate = new FlipCandidate { ProfitPerUnit = int.MinValue };

        Assert.Equal(int.MinValue, candidate.ProfitPerUnit);
        Assert.False(candidate.IsProfitable);
    }

    [Fact]
    public void ProfitPerUnit_Int32MaxValue_HandlesCorrectly()
    {
        var candidate = new FlipCandidate { ProfitPerUnit = int.MaxValue };

        Assert.Equal(int.MaxValue, candidate.ProfitPerUnit);
        Assert.True(candidate.IsProfitable);
    }
}
