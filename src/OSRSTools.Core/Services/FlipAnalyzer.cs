using Microsoft.Extensions.Logging;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;

namespace OSRSTools.Core.Services;

/// <summary>
/// Orchestrator that produces a ranked list of flip candidates.
/// Pipeline: fetch data -> filter -> recommend prices -> calculate flip -> detect manipulation -> score -> rank.
/// </summary>
public class FlipAnalyzer : IFlipAnalyzer
{
    private readonly IDataFetchService _dataFetchService;
    private readonly IPriceRecommendationService _priceRecommendationService;
    private readonly IFlipCalculator _flipCalculator;
    private readonly IScoringService _scoringService;
    private readonly IManipulationDetector _manipulationDetector;
    private readonly ILogger<FlipAnalyzer> _logger;

    public FlipAnalyzer(
        IDataFetchService dataFetchService,
        IPriceRecommendationService priceRecommendationService,
        IFlipCalculator flipCalculator,
        IScoringService scoringService,
        IManipulationDetector manipulationDetector,
        ILogger<FlipAnalyzer> logger)
    {
        _dataFetchService = dataFetchService;
        _priceRecommendationService = priceRecommendationService;
        _flipCalculator = flipCalculator;
        _scoringService = scoringService;
        _manipulationDetector = manipulationDetector;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FlipCandidate>> AnalyzeFlipsAsync(
        FlipSettings settings, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing flip candidates...");

        var mappingsTask = _dataFetchService.GetMappingsAsync(cancellationToken);
        var pricesTask = _dataFetchService.GetCompletePriceDataAsync(cancellationToken);
        await Task.WhenAll(mappingsTask, pricesTask);
        var mappings = await mappingsTask;
        var prices = await pricesTask;

        var candidates = new List<FlipCandidate>();

        foreach (var (itemId, mapping) in mappings)
        {
            if (!prices.TryGetValue(itemId, out var priceData))
                continue;

            if (priceData.Volume24Hr < settings.MinVolume)
                continue;

            if (mapping.BuyLimit < settings.MinBuyLimit)
                continue;

            var recommendation = _priceRecommendationService.CalculateRecommendedPrices(priceData);

            if (!recommendation.HasSufficientData)
                continue;

            if (recommendation.GrossMargin < settings.MinMargin)
                continue;

            if (_manipulationDetector.IsSuspicious(priceData, 50.0))
                continue;

            var candidate = _flipCalculator.CalculateFlip(
                itemId, mapping.Name, mapping.Members, mapping.BuyLimit,
                recommendation, priceData.Volume24Hr, settings);

            if (!candidate.IsProfitable)
                continue;

            candidate.FlipScore = _scoringService.CalculateFlipScore(candidate);
            candidate.ConfidenceRating = _scoringService.CalculateConfidence(
                recommendation.WindowsUsedForBuy, priceData.Volume24Hr);

            candidates.Add(candidate);
        }

        var ranked = candidates
            .OrderByDescending(c => c.GpPerHour)
            .Take(settings.MaxResults)
            .ToList();

        _logger.LogInformation(
            "Analyzed {Total} flip candidates, returning top {Count}",
            candidates.Count, ranked.Count);

        return ranked;
    }
}
