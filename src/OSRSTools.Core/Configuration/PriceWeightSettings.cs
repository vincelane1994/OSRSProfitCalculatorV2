namespace OSRSTools.Core.Configuration;

public class PriceWeightSettings
{
    public double FiveMinute { get; set; } = 0.50;
    public double OneHour { get; set; } = 0.30;
    public double SixHour { get; set; } = 0.15;
    public double TwentyFourHour { get; set; } = 0.05;
}
