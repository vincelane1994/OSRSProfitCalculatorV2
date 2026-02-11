using System.Text.Json.Serialization;

namespace OSRSTools.Infrastructure.Api.Dtos;

/// <summary>
/// DTO for deserializing the /latest endpoint response.
/// </summary>
public class LatestPricesDto
{
    [JsonPropertyName("data")]
    public Dictionary<string, LatestItemPriceDto> Data { get; set; } = new();
}

/// <summary>
/// DTO for a single item's latest prices.
/// "high" = instant-buy price, "low" = instant-sell price.
/// </summary>
public class LatestItemPriceDto
{
    [JsonPropertyName("high")]
    public int? High { get; set; }

    [JsonPropertyName("highTime")]
    public long? HighTime { get; set; }

    [JsonPropertyName("low")]
    public int? Low { get; set; }

    [JsonPropertyName("lowTime")]
    public long? LowTime { get; set; }
}
