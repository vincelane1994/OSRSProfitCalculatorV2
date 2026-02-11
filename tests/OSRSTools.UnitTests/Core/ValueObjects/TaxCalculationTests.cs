using OSRSTools.Core.ValueObjects;
using Xunit;

namespace OSRSTools.UnitTests.Core.ValueObjects;

public class TaxCalculationTests
{
    private const double DefaultRate = 0.02;
    private const long DefaultCap = 5_000_000;

    [Fact]
    public void Calculate_StandardSellPrice_ReturnsFloorBasedTax()
    {
        var result = TaxCalculation.Calculate(1000, DefaultRate, DefaultCap);

        Assert.Equal(20, result.TaxAmount); // floor(1000 * 0.02) = 20
        Assert.Equal(980, result.NetAfterTax);
        Assert.False(result.WasCapped);
    }

    [Fact]
    public void Calculate_HighSellPrice_CapsAtTaxCap()
    {
        var result = TaxCalculation.Calculate(500_000_000, DefaultRate, DefaultCap);

        Assert.Equal(5_000_000, result.TaxAmount); // 500M * 0.02 = 10M, capped at 5M
        Assert.Equal(495_000_000, result.NetAfterTax);
        Assert.True(result.WasCapped);
    }

    [Fact]
    public void Calculate_ExactlyAtCap_IsNotCapped()
    {
        // 250M * 0.02 = 5M exactly = cap
        var result = TaxCalculation.Calculate(250_000_000, DefaultRate, DefaultCap);

        Assert.Equal(5_000_000, result.TaxAmount);
        Assert.False(result.WasCapped);
    }

    [Fact]
    public void Calculate_JustAboveCap_IsCapped()
    {
        // 300M * 0.02 = 6M, which exceeds 5M cap
        var result = TaxCalculation.Calculate(300_000_000, DefaultRate, DefaultCap);

        Assert.Equal(5_000_000, result.TaxAmount);
        Assert.True(result.WasCapped);
    }

    [Fact]
    public void Calculate_ZeroSellPrice_ReturnsZeroTax()
    {
        var result = TaxCalculation.Calculate(0, DefaultRate, DefaultCap);

        Assert.Equal(0, result.TaxAmount);
        Assert.Equal(0, result.NetAfterTax);
        Assert.False(result.WasCapped);
    }

    [Fact]
    public void Calculate_SmallSellPrice_FloorsTax()
    {
        // 49 * 0.02 = 0.98, floor = 0
        var result = TaxCalculation.Calculate(49, DefaultRate, DefaultCap);

        Assert.Equal(0, result.TaxAmount);
        Assert.Equal(49, result.NetAfterTax);
    }

    [Fact]
    public void Calculate_SellPriceStoredCorrectly()
    {
        var result = TaxCalculation.Calculate(5000, DefaultRate, DefaultCap);

        Assert.Equal(5000, result.SellPrice);
    }

    [Fact]
    public void Calculate_CustomRate_UsesProvidedRate()
    {
        var result = TaxCalculation.Calculate(1000, 0.05, DefaultCap);

        Assert.Equal(50, result.TaxAmount); // floor(1000 * 0.05) = 50
    }

    [Fact]
    public void Calculate_CustomCap_UsesProvidedCap()
    {
        var result = TaxCalculation.Calculate(100_000, DefaultRate, 100);

        Assert.Equal(100, result.TaxAmount); // floor(100K * 0.02) = 2000, capped at 100
        Assert.True(result.WasCapped);
    }
}
