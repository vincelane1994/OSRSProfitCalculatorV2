using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OSRSTools.Core.Configuration;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;
using OSRSTools.Infrastructure.Api.Dtos;

namespace OSRSTools.Infrastructure.Api;

/// <summary>
/// HTTP client for the OSRS Wiki Real-Time Prices API.
/// Implements both IItemMappingRepository and IPriceRepository.
/// </summary>
public class OsrsWikiApiClient : IItemMappingRepository, IPriceRepository
{
    private readonly HttpClient _httpClient;
    private readonly OsrsApiSettings _settings;
    private readonly ILogger<OsrsWikiApiClient> _logger;

    // Local cache for high alch values from /mapping (not in domain entities)
    private readonly Dictionary<int, int?> _highAlchValues = new();

    public OsrsWikiApiClient(
        HttpClient httpClient,
        IOptions<OsrsApiSettings> settings,
        ILogger<OsrsWikiApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_settings.UserAgent);
    }

    public async Task<IReadOnlyDictionary<int, ItemMapping>> GetAllMappingsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching item mappings from {Endpoint}", _settings.Endpoints.Mapping);

        var dtos = await _httpClient.GetFromJsonAsync<List<ItemMappingDto>>(
            _settings.Endpoints.Mapping, cancellationToken);

        if (dtos == null || dtos.Count == 0)
        {
            _logger.LogWarning("No item mappings returned from API");
            return new Dictionary<int, ItemMapping>();
        }

        var mappings = new Dictionary<int, ItemMapping>(dtos.Count);

        foreach (var dto in dtos)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                _logger.LogWarning("Skipping item {ItemId}: empty or null name", dto.Id);
                continue;
            }

            if (dto.HighAlch.HasValue && dto.HighAlch.Value < 0)
            {
                _logger.LogWarning("Item {ItemId}: negative high alch value {Value}, treating as null", dto.Id, dto.HighAlch.Value);
                dto.HighAlch = null;
            }

            mappings[dto.Id] = new ItemMapping
            {
                ItemId = dto.Id,
                Name = dto.Name,
                BuyLimit = dto.Limit ?? 0,
                Members = dto.Members,
                Examine = dto.Examine,
                Icon = dto.Icon
            };

            // Store high alch values separately (not part of ItemMapping entity)
            _highAlchValues[dto.Id] = dto.HighAlch;
        }

        _logger.LogInformation("Fetched {Count} item mappings", mappings.Count);
        return mappings;
    }

    public async Task<ItemMapping?> GetByIdAsync(int itemId, CancellationToken cancellationToken = default)
    {
        var allMappings = await GetAllMappingsAsync(cancellationToken);
        return allMappings.TryGetValue(itemId, out var mapping) ? mapping : null;
    }

    public async Task<IReadOnlyDictionary<int, ItemPriceData>> GetLatestPricesAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching latest prices from {Endpoint}", _settings.Endpoints.Latest);

        var dto = await _httpClient.GetFromJsonAsync<LatestPricesDto>(
            _settings.Endpoints.Latest, cancellationToken);

        if (dto?.Data == null || dto.Data.Count == 0)
        {
            _logger.LogWarning("No latest prices returned from API");
            return new Dictionary<int, ItemPriceData>();
        }

        var prices = new Dictionary<int, ItemPriceData>(dto.Data.Count);

        foreach (var (itemIdStr, priceDto) in dto.Data)
        {
            if (!int.TryParse(itemIdStr, out var itemId))
            {
                _logger.LogWarning("Skipping latest price entry with non-numeric item ID: {ItemId}", itemIdStr);
                continue;
            }

            if ((priceDto.High.HasValue && priceDto.High.Value < 0) ||
                (priceDto.Low.HasValue && priceDto.Low.Value < 0))
            {
                _logger.LogWarning("Skipping item {ItemId}: negative price (high={High}, low={Low})", itemId, priceDto.High, priceDto.Low);
                continue;
            }

            prices[itemId] = new ItemPriceData
            {
                ItemId = itemId,
                LatestBuyPrice = priceDto.High,
                LatestSellPrice = priceDto.Low,
                LatestBuyTime = priceDto.HighTime.HasValue
                    ? DateTimeOffset.FromUnixTimeSeconds(priceDto.HighTime.Value).UtcDateTime
                    : null,
                LatestSellTime = priceDto.LowTime.HasValue
                    ? DateTimeOffset.FromUnixTimeSeconds(priceDto.LowTime.Value).UtcDateTime
                    : null
            };
        }

        _logger.LogInformation("Fetched latest prices for {Count} items", prices.Count);
        return prices;
    }

    public async Task<IReadOnlyDictionary<int, TimeWindowPrice>> GetTimeWindowPricesAsync(
        TimeWindow window, CancellationToken cancellationToken = default)
    {
        var endpoint = GetEndpointForWindow(window);
        _logger.LogInformation("Fetching {Window} prices from {Endpoint}", window, endpoint);

        var dto = await _httpClient.GetFromJsonAsync<TimeWindowPricesDto>(
            endpoint, cancellationToken);

        if (dto?.Data == null || dto.Data.Count == 0)
        {
            _logger.LogWarning("No {Window} prices returned from API", window);
            return new Dictionary<int, TimeWindowPrice>();
        }

        var prices = new Dictionary<int, TimeWindowPrice>(dto.Data.Count);

        foreach (var (itemIdStr, priceDto) in dto.Data)
        {
            if (!int.TryParse(itemIdStr, out var itemId))
            {
                _logger.LogWarning("Skipping {Window} price entry with non-numeric item ID: {ItemId}", window, itemIdStr);
                continue;
            }

            if ((priceDto.AvgHighPrice.HasValue && priceDto.AvgHighPrice.Value < 0) ||
                (priceDto.AvgLowPrice.HasValue && priceDto.AvgLowPrice.Value < 0))
            {
                _logger.LogWarning("Skipping item {ItemId}: negative {Window} price (avgHigh={High}, avgLow={Low})", itemId, window, priceDto.AvgHighPrice, priceDto.AvgLowPrice);
                continue;
            }

            if ((priceDto.HighPriceVolume.HasValue && priceDto.HighPriceVolume.Value < 0) ||
                (priceDto.LowPriceVolume.HasValue && priceDto.LowPriceVolume.Value < 0))
            {
                _logger.LogWarning("Skipping item {ItemId}: negative {Window} volume (highVol={HighVol}, lowVol={LowVol})", itemId, window, priceDto.HighPriceVolume, priceDto.LowPriceVolume);
                continue;
            }

            if (priceDto.HighPriceVolume.HasValue && priceDto.HighPriceVolume.Value > int.MaxValue)
                _logger.LogWarning("Item {ItemId}: HighPriceVolume {Volume} exceeds int.MaxValue, capping", itemId, priceDto.HighPriceVolume.Value);
            if (priceDto.LowPriceVolume.HasValue && priceDto.LowPriceVolume.Value > int.MaxValue)
                _logger.LogWarning("Item {ItemId}: LowPriceVolume {Volume} exceeds int.MaxValue, capping", itemId, priceDto.LowPriceVolume.Value);

            prices[itemId] = new TimeWindowPrice
            {
                AvgBuyPrice = priceDto.AvgHighPrice,
                AvgSellPrice = priceDto.AvgLowPrice,
                BuyVolume = priceDto.HighPriceVolume.HasValue ? (int)Math.Min(priceDto.HighPriceVolume.Value, int.MaxValue) : null,
                SellVolume = priceDto.LowPriceVolume.HasValue ? (int)Math.Min(priceDto.LowPriceVolume.Value, int.MaxValue) : null
            };
        }

        _logger.LogInformation("Fetched {Window} prices for {Count} items", window, prices.Count);
        return prices;
    }

    /// <summary>
    /// Gets the high alchemy value for an item (loaded from /mapping).
    /// Returns null if mappings haven't been fetched yet or item has no alch value.
    /// </summary>
    public int? GetHighAlchValue(int itemId)
    {
        return _highAlchValues.TryGetValue(itemId, out var value) ? value : null;
    }

    private string GetEndpointForWindow(TimeWindow window)
    {
        return window switch
        {
            TimeWindow.FiveMinute => _settings.Endpoints.FiveMinute,
            TimeWindow.OneHour => _settings.Endpoints.OneHour,
            TimeWindow.SixHour => _settings.Endpoints.SixHour,
            TimeWindow.TwentyFourHour => _settings.Endpoints.TwentyFourHour,
            _ => throw new ArgumentOutOfRangeException(nameof(window), window, "Unknown time window")
        };
    }
}
