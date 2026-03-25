namespace OSRSTools.Core.Entities;

/// <summary>
/// Types of herblore operations that can be evaluated for profitability.
/// </summary>
public enum HerbloreMethod
{
    /// <summary>Grimy herb → Clean herb. Profit = cleanPrice - grimyPrice.</summary>
    Cleaning,

    /// <summary>Grimy herb + Vial of water → Unfinished potion. Profit = unfPrice - grimyPrice - vialPrice.</summary>
    FullProcess,

    /// <summary>Clean herb + Secondary ingredient → Finished potion. Profit = potionPrice - cleanPrice - secondaryPrice.</summary>
    PotionMaking
}
