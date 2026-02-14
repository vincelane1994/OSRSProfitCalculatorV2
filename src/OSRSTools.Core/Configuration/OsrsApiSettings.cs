namespace OSRSTools.Core.Configuration;

public class OsrsApiSettings
{
    public string BaseUrl { get; set; } = "https://prices.runescape.wiki/api/v1/osrs/";
    public string UserAgent { get; set; } = "OSRSProfitCalculatorV2/1.0 - Personal Use";
    public ApiEndpointSettings Endpoints { get; set; } = new();
}

public class ApiEndpointSettings
{
    public string Mapping { get; set; } = "mapping";
    public string Latest { get; set; } = "latest";
    public string FiveMinute { get; set; } = "5m";
    public string OneHour { get; set; } = "1h";
    public string SixHour { get; set; } = "6h";
    public string TwentyFourHour { get; set; } = "24h";
}
