namespace OSRSTools.Core.Configuration;

public class TaxSettings
{
    public double Rate { get; set; } = 0.02;
    public long Cap { get; set; } = 5_000_000;
}
