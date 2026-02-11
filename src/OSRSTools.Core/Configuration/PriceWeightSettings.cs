namespace OSRSTools.Core.Configuration;

public class PriceWeightSettings
{
    public double FiveMinute { get; set; } = 0.10;
    public double OneHour { get; set; } = 0.35;
    public double SixHour { get; set; } = 0.35;
    public double TwentyFourHour { get; set; } = 0.20;
}
