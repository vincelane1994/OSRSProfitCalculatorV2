using System.Text.Json.Serialization;

namespace OSRSTools.Infrastructure.Api.Dtos;

/// <summary>
/// DTO for deserializing the /5m, /1h, /6h, /24h endpoint responses.
/// </summary>
public class TimeWindowPricesDto
{
    [JsonPropertyName("data")]
    public Dictionary<string, TimeWindowItemPriceDto> Data { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}

/// <summary>
/// DTO for a single item's time window prices and volumes.
/// </summary>
public class TimeWindowItemPriceDto
{
    [JsonPropertyName("avgHighPrice")]
    public int? AvgHighPrice { get; set; }

    [JsonPropertyName("highPriceVolume")]
    public long? HighPriceVolume { get; set; }

    [JsonPropertyName("avgLowPrice")]
    public int? AvgLowPrice { get; set; }

    [JsonPropertyName("lowPriceVolume")]
    public long? LowPriceVolume { get; set; }
}
