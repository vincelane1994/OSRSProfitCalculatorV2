namespace OSRSTools.Core.Entities;

/// <summary>
/// Reference data for an OSRS item from the /mapping endpoint.
/// Contains static item information that rarely changes.
/// </summary>
public class ItemMapping
{
    public int ItemId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int BuyLimit { get; init; }
    public bool Members { get; init; }
    public string? Examine { get; init; }
    public string? Icon { get; init; }
}
