using OSRSTools.Core.ValueObjects;
using Xunit;

namespace OSRSTools.UnitTests.Core.ValueObjects;

public class ProfitCalculationTests
{
    [Fact]
    public void IsProfitable_PositiveProfit_ReturnsTrue()
    {
        var calc = new ProfitCalculation { ProfitPerUnit = 100 };
        Assert.True(calc.IsProfitable);
    }

    [Fact]
    public void IsProfitable_ZeroProfit_ReturnsFalse()
    {
        var calc = new ProfitCalculation { ProfitPerUnit = 0 };
        Assert.False(calc.IsProfitable);
    }

    [Fact]
    public void IsProfitable_NegativeProfit_ReturnsFalse()
    {
        var calc = new ProfitCalculation { ProfitPerUnit = -50 };
        Assert.False(calc.IsProfitable);
    }

    [Fact]
    public void DefaultValues_AreAllZero()
    {
        var calc = new ProfitCalculation();

        Assert.Equal(0, calc.ProfitPerUnit);
        Assert.Equal(0, calc.CostPerUnit);
        Assert.Equal(0, calc.RevenuePerUnit);
        Assert.Equal(0, calc.Quantity);
        Assert.Equal(0, calc.TotalInvestment);
        Assert.Equal(0, calc.TotalProfit);
        Assert.Equal(0, calc.RoiPercent);
    }
}
