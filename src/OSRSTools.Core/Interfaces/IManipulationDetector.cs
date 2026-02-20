using OSRSTools.Core.Entities;

namespace OSRSTools.Core.Interfaces;

/// <summary>
/// Detects if an item's price data shows signs of market manipulation.
/// Checks for: price deviation between short and long term windows,
/// extreme buy/sell volume ratios.
/// </summary>
public interface IManipulationDetector
{
    /// <summary>
    /// Detects if an item's price data shows signs of market manipulation.
    /// </summary>
    /// <param name="priceData">The item's price data across all time windows.</param>
    /// <param name="deviationThresholdPercent">Max allowed % deviation between 5m and 24h prices (default 50).</param>
    /// <returns>True if the item appears to be manipulated.</returns>
    bool IsSuspicious(ItemPriceData priceData, double deviationThresholdPercent = 50.0);
}
