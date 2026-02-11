namespace OSRSTools.Core.ValueObjects;

/// <summary>
/// Immutable result of a GE tax calculation.
/// Tax = floor(sellPrice * taxRate), capped at taxCap.
/// </summary>
public readonly record struct TaxCalculation
{
    public int SellPrice { get; init; }
    public int TaxAmount { get; init; }
    public int NetAfterTax { get; init; }
    public bool WasCapped { get; init; }

    /// <summary>
    /// Calculates the GE tax for a given sell price.
    /// </summary>
    public static TaxCalculation Calculate(int sellPrice, double taxRate, long taxCap)
    {
        var rawTax = (long)Math.Floor(sellPrice * taxRate);
        var wasCapped = rawTax > taxCap;
        var finalTax = wasCapped ? (int)taxCap : (int)rawTax;

        return new TaxCalculation
        {
            SellPrice = sellPrice,
            TaxAmount = finalTax,
            NetAfterTax = sellPrice - finalTax,
            WasCapped = wasCapped
        };
    }
}
