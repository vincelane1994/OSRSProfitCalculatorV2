using Microsoft.Extensions.Options;
using OSRSTools.Core.Configuration;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;

namespace OSRSTools.Core.Services;

/// <summary>
/// Detects potential market manipulation by comparing short-term and long-term
/// price data, checking for extreme volume imbalances, and flagging
/// high-ROI items with suspiciously low volume.
/// </summary>
public class ManipulationDetector : IManipulationDetector
{
    private readonly ManipulationSettings _settings;

    public ManipulationDetector(IOptions<ManipulationSettings> settings)
    {
        _settings = settings.Value;
    }

    public bool IsSuspicious(ItemPriceData priceData, double roiPercent = 0)
    {
        if (HasPriceDeviation(priceData))
            return true;

        if (HasExtremeVolumeRatio(priceData))
            return true;

        if (HasHighRoiLowVolume(priceData, roiPercent))
            return true;

        return false;
    }

    private bool HasPriceDeviation(ItemPriceData priceData)
    {
        var has5m = priceData.TimeWindows.TryGetValue(TimeWindow.FiveMinute, out var window5m);
        var has24h = priceData.TimeWindows.TryGetValue(TimeWindow.TwentyFourHour, out var window24h);

        if (!has5m || !has24h || window5m is null || window24h is null) return false;

        if (window5m!.AvgBuyPrice.HasValue && window24h!.AvgBuyPrice.HasValue
            && window24h.AvgBuyPrice.Value > 0)
        {
            var deviation = Math.Abs((double)(window5m.AvgBuyPrice.Value - window24h.AvgBuyPrice.Value)
                / window24h.AvgBuyPrice.Value * 100.0);
            if (deviation > _settings.PriceDeviationThresholdPercent) return true;
        }

        if (window5m.AvgSellPrice.HasValue && window24h!.AvgSellPrice.HasValue
            && window24h.AvgSellPrice.Value > 0)
        {
            var deviation = Math.Abs((double)(window5m.AvgSellPrice.Value - window24h.AvgSellPrice.Value)
                / window24h.AvgSellPrice.Value * 100.0);
            if (deviation > _settings.PriceDeviationThresholdPercent) return true;
        }

        return false;
    }

    private bool HasExtremeVolumeRatio(ItemPriceData priceData)
    {
        if (!priceData.TimeWindows.TryGetValue(TimeWindow.TwentyFourHour, out var window24h))
            return false;

        var buyVol = window24h.BuyVolume ?? 0;
        var sellVol = window24h.SellVolume ?? 0;

        if (buyVol == 0 || sellVol == 0) return false;

        var ratio = (double)Math.Max(buyVol, sellVol) / Math.Min(buyVol, sellVol);
        return ratio > _settings.VolumeRatioThreshold;
    }

    private bool HasHighRoiLowVolume(ItemPriceData priceData, double roiPercent)
    {
        if (!priceData.TimeWindows.TryGetValue(TimeWindow.TwentyFourHour, out var w24h))
            return false;

        var volume = (w24h.BuyVolume ?? 0) + (w24h.SellVolume ?? 0);
        return roiPercent > _settings.HighRoiThresholdPercent
            && volume < _settings.LowVolumeThreshold;
    }
}
