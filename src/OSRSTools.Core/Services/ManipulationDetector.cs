using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;

namespace OSRSTools.Core.Services;

/// <summary>
/// Detects potential market manipulation by comparing short-term and long-term
/// price data and checking for extreme volume imbalances.
/// </summary>
public class ManipulationDetector : IManipulationDetector
{
    private const double VolumeRatioThreshold = 10.0;

    public bool IsSuspicious(ItemPriceData priceData, double deviationThresholdPercent = 50.0)
    {
        if (HasPriceDeviation(priceData, deviationThresholdPercent))
            return true;

        if (HasExtremeVolumeRatio(priceData))
            return true;

        return false;
    }

    private static bool HasPriceDeviation(ItemPriceData priceData, double thresholdPercent)
    {
        var has5m = priceData.TimeWindows.TryGetValue(TimeWindow.FiveMinute, out var window5m);
        var has24h = priceData.TimeWindows.TryGetValue(TimeWindow.TwentyFourHour, out var window24h);

        if (!has5m || !has24h) return false;

        if (window5m!.AvgBuyPrice.HasValue && window24h!.AvgBuyPrice.HasValue
            && window24h.AvgBuyPrice.Value > 0)
        {
            var deviation = Math.Abs((double)(window5m.AvgBuyPrice.Value - window24h.AvgBuyPrice.Value)
                / window24h.AvgBuyPrice.Value * 100.0);
            if (deviation > thresholdPercent) return true;
        }

        if (window5m.AvgSellPrice.HasValue && window24h!.AvgSellPrice.HasValue
            && window24h.AvgSellPrice.Value > 0)
        {
            var deviation = Math.Abs((double)(window5m.AvgSellPrice.Value - window24h.AvgSellPrice.Value)
                / window24h.AvgSellPrice.Value * 100.0);
            if (deviation > thresholdPercent) return true;
        }

        return false;
    }

    private static bool HasExtremeVolumeRatio(ItemPriceData priceData)
    {
        if (!priceData.TimeWindows.TryGetValue(TimeWindow.TwentyFourHour, out var window24h))
            return false;

        var buyVol = window24h.BuyVolume ?? 0;
        var sellVol = window24h.SellVolume ?? 0;

        if (buyVol == 0 || sellVol == 0) return false;

        var ratio = (double)Math.Max(buyVol, sellVol) / Math.Min(buyVol, sellVol);
        return ratio > VolumeRatioThreshold;
    }
}
