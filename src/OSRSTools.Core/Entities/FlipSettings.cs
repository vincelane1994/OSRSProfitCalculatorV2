namespace OSRSTools.Core.Entities;

/// <summary>
/// User-configurable settings for filtering and displaying flip candidates.
/// </summary>
public class FlipSettings
{
    /// <summary>Minimum 24-hour volume required to consider an item.</summary>
    public int MinVolume { get; set; } = 10_000;

    /// <summary>Minimum GE buy limit required.</summary>
    public int MinBuyLimit { get; set; } = 100;

    /// <summary>Minimum margin (sell - buy) in GP to consider an item.</summary>
    public int MinMargin { get; set; } = 10;

    /// <summary>Maximum total investment in GP (buy price * quantity cap).</summary>
    public long MaxInvestment { get; set; } = 10_000_000;

    /// <summary>Buy limit cycle duration in hours (default 4-hour GE cycle).</summary>
    public double BuyLimitCycleHours { get; set; } = 4.0;

    /// <summary>Maximum number of results to display.</summary>
    public int MaxResults { get; set; } = 100;
}
