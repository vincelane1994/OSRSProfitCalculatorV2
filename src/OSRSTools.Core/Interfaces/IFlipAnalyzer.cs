using OSRSTools.Core.Entities;

namespace OSRSTools.Core.Interfaces;

/// <summary>
/// Analyzes all tradeable items for flip profitability.
/// Pipeline: fetch -> price recommendation -> calculate flip -> filter -> detect manipulation -> score -> rank.
/// Returns top candidates sorted by GP/hr descending.
/// </summary>
public interface IFlipAnalyzer
{
    Task<IReadOnlyList<FlipCandidate>> AnalyzeFlipsAsync(
        FlipSettings settings,
        CancellationToken cancellationToken = default);
}
