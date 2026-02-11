using System.Text.Json.Serialization;

namespace OSRSTools.Infrastructure.Api.Dtos;

/// <summary>
/// DTO for deserializing a single item from the /mapping endpoint.
/// </summary>
public class ItemMappingDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("members")]
    public bool Members { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("highalch")]
    public int? HighAlch { get; set; }

    [JsonPropertyName("examine")]
    public string? Examine { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
}
