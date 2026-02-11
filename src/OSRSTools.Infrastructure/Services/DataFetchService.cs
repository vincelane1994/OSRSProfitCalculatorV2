using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OSRSTools.Core.Configuration;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;
using OSRSTools.Infrastructure.Api;

namespace OSRSTools.Infrastructure.Services;

/// <summary>
/// Orchestrator that coordinates fetching item mappings, latest prices,
/// and all time window data, assembling complete ItemPriceData objects.
/// Uses caching to avoid redundant API calls.
/// </summary>
public class DataFetchService : IDataFetchService
{
    private readonly IItemMappingRepository _mappingRepository;
    private readonly IPriceRepository _priceRepository;
    private readonly ICacheService _cache;
    private readonly OsrsWikiApiClient _apiClient;
    private readonly CacheSettings _cacheSettings;
    private readonly ILogger<DataFetchService> _logger;

    private const string MappingsCacheKey = "item_mappings";
    private const string CompletePricesCacheKey = "complete_prices";

    public DataFetchService(
        IItemMappingRepository mappingRepository,
        IPriceRepository priceRepository,
        ICacheService cache,
        OsrsWikiApiClient apiClient,
        IOptions<CacheSettings> cacheSettings,
        ILogger<DataFetchService> logger)
    {
        _mappingRepository = mappingRepository;
        _priceRepository = priceRepository;
        _cache = cache;
        _apiClient = apiClient;
        _cacheSettings = cacheSettings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<int, ItemMapping>> GetMappingsAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGet<IReadOnlyDictionary<int, ItemMapping>>(MappingsCacheKey, out var cached) && cached != null)
        {
            _logger.LogDebug("Returning cached mappings ({Count} items)", cached.Count);
            return cached;
        }

        var mappings = await _mappingRepository.GetAllMappingsAsync(cancellationToken);
        _cache.Set(MappingsCacheKey, mappings, _cacheSettings.MappingDuration);

        _logger.LogInformation("Cached {Count} item mappings for {Duration}", mappings.Count, _cacheSettings.MappingDuration);
        return mappings;
    }

    public async Task<IReadOnlyDictionary<int, ItemPriceData>> GetCompletePriceDataAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGet<IReadOnlyDictionary<int, ItemPriceData>>(CompletePricesCacheKey, out var cached) && cached != null)
        {
            _logger.LogDebug("Returning cached price data ({Count} items)", cached.Count);
            return cached;
        }

        _logger.LogInformation("Fetching complete price data from API...");

        // Fetch latest prices and all time windows concurrently
        var latestTask = _priceRepository.GetLatestPricesAsync(cancellationToken);
        var fiveMinTask = _priceRepository.GetTimeWindowPricesAsync(TimeWindow.FiveMinute, cancellationToken);
        var oneHourTask = _priceRepository.GetTimeWindowPricesAsync(TimeWindow.OneHour, cancellationToken);
        var sixHourTask = _priceRepository.GetTimeWindowPricesAsync(TimeWindow.SixHour, cancellationToken);
        var twentyFourHourTask = _priceRepository.GetTimeWindowPricesAsync(TimeWindow.TwentyFourHour, cancellationToken);

        await Task.WhenAll(latestTask, fiveMinTask, oneHourTask, sixHourTask, twentyFourHourTask);

        var latestPrices = await latestTask;
        var fiveMinPrices = await fiveMinTask;
        var oneHourPrices = await oneHourTask;
        var sixHourPrices = await sixHourTask;
        var twentyFourHourPrices = await twentyFourHourTask;

        // Merge all data into complete ItemPriceData objects
        var allItemIds = latestPrices.Keys
            .Union(fiveMinPrices.Keys)
            .Union(oneHourPrices.Keys)
            .Union(sixHourPrices.Keys)
            .Union(twentyFourHourPrices.Keys)
            .Distinct();

        var result = new Dictionary<int, ItemPriceData>();

        foreach (var itemId in allItemIds)
        {
            var timeWindows = new Dictionary<TimeWindow, TimeWindowPrice>();

            if (fiveMinPrices.TryGetValue(itemId, out var fiveMin))
                timeWindows[TimeWindow.FiveMinute] = fiveMin;
            if (oneHourPrices.TryGetValue(itemId, out var oneHour))
                timeWindows[TimeWindow.OneHour] = oneHour;
            if (sixHourPrices.TryGetValue(itemId, out var sixHour))
                timeWindows[TimeWindow.SixHour] = sixHour;
            if (twentyFourHourPrices.TryGetValue(itemId, out var twentyFourHour))
                timeWindows[TimeWindow.TwentyFourHour] = twentyFourHour;

            latestPrices.TryGetValue(itemId, out var latest);

            result[itemId] = new ItemPriceData
            {
                ItemId = itemId,
                LatestBuyPrice = latest?.LatestBuyPrice,
                LatestSellPrice = latest?.LatestSellPrice,
                LatestBuyTime = latest?.LatestBuyTime,
                LatestSellTime = latest?.LatestSellTime,
                TimeWindows = timeWindows
            };
        }

        _cache.Set(CompletePricesCacheKey, (IReadOnlyDictionary<int, ItemPriceData>)result, _cacheSettings.PriceDuration);
        _logger.LogInformation("Assembled and cached complete price data for {Count} items", result.Count);

        return result;
    }

    public async Task<int?> GetHighAlchValueAsync(int itemId, CancellationToken cancellationToken = default)
    {
        // Ensure mappings are loaded (which also loads high alch values in the API client)
        await GetMappingsAsync(cancellationToken);
        return _apiClient.GetHighAlchValue(itemId);
    }
}
