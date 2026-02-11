using OSRSTools.Core.Entities;

namespace OSRSTools.Core.ValueObjects;

/// <summary>
/// Immutable result of the weighted price recommendation calculation.
///
/// For a patient flip:
/// - RecommendedBuyPrice derives from instant-sell ("low") data — placing buy offers near where sellers dump.
/// - RecommendedSellPrice derives from instant-buy ("high") data — placing sell offers near where buyers overpay.
///
/// Missing data handling: if a time window returns null, its weight is
/// redistributed proportionally across remaining windows.
/// </summary>
public readonly record struct PriceRecommendation
{
    public int RecommendedBuyPrice { get; init; }
    public int RecommendedSellPrice { get; init; }
    public int WindowsUsedForBuy { get; init; }
    public int WindowsUsedForSell { get; init; }

    /// <summary>
    /// Whether there is sufficient data (at least 2 windows) for both buy and sell.
    /// Items with fewer than 2 windows should be excluded from candidates.
    /// </summary>
    public bool HasSufficientData => WindowsUsedForBuy >= 2 && WindowsUsedForSell >= 2;

    /// <summary>Gross margin before tax.</summary>
    public int GrossMargin => RecommendedSellPrice - RecommendedBuyPrice;
}
