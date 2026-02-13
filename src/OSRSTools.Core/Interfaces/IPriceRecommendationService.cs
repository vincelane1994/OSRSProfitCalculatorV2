using OSRSTools.Core.Entities;
using OSRSTools.Core.ValueObjects;

namespace OSRSTools.Core.Interfaces;

/// <summary>
/// Calculates weighted recommended buy/sell prices from time window data.
/// Dedicated service for the flipping pipeline, decoupled from general profit calculations.
/// Contains no I/O — all data is passed in as parameters.
/// </summary>
public interface IPriceRecommendationService
{
    /// <summary>
    /// Calculates weighted recommended buy/sell prices from time window data.
    /// Buy price = weighted avg of instant-sell prices (where sellers dump).
    /// Sell price = weighted avg of instant-buy prices (where buyers overpay).
    /// Missing windows have their weight redistributed proportionally.
    /// </summary>
    PriceRecommendation CalculateRecommendedPrices(ItemPriceData priceData);
}
