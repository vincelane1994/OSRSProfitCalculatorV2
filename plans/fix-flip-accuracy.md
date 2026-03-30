# Fix Flip Accuracy — Implementation Plan

**Goal:** Address the 5 critical issues identified in the flip calculator review to produce
recommendations that reflect real-world flipping outcomes.

---

## Fix 1 — Re-weight Prices Toward Recent Data

**Problem:** Current weights (5m=10%, 1h=35%, 6h=35%, 24h=20%) give 55% to stale data.
A flipper needs the current spread, not yesterday's average. The 5m window is the most
relevant signal and gets the least weight.

**Change:** Update weights to (5m=50%, 1h=30%, 6h=15%, 24h=5%).

**Files:**
- `Core/Configuration/PriceWeightSettings.cs` — update default values
- `Web/appsettings.json` — update `PriceWeights` section

**Changes:**
```csharp
// PriceWeightSettings.cs
public double FiveMinute { get; set; } = 0.50;    // was 0.10
public double OneHour { get; set; } = 0.30;       // was 0.35
public double SixHour { get; set; } = 0.15;       // was 0.35
public double TwentyFourHour { get; set; } = 0.05; // was 0.20
```

```json
"PriceWeights": {
    "FiveMinute": 0.50,
    "OneHour": 0.30,
    "SixHour": 0.15,
    "TwentyFourHour": 0.05
}
```

**Tests:** No test changes needed — `PriceRecommendationService` tests inject their own
config. Run existing tests to confirm.

**Dependencies:** None.

---

## Fix 2 — Re-add Manipulation Detection

**Problem:** Manipulation detection was removed from the FlipAnalyzer pipeline. Items
mid-pump-and-dump show huge margins and score well, leading users into traps.

**Change:** Re-add the `_manipulationDetector.IsSuspicious()` call in `FlipAnalyzer` after
the flip calculation (needs `RoiPercent`) and before scoring.

**Files:**
- `Core/Services/FlipAnalyzer.cs` — add manipulation check back to pipeline

**Changes:**

Insert after the `IsProfitable` check and before scoring:
```csharp
if (_manipulationDetector.IsSuspicious(priceData, candidate.RoiPercent))
    continue;
```

Pipeline order becomes:
1. No price data → skip
2. Volume < MinVolume → skip
3. Buy limit < MinBuyLimit → skip
4. Price recommendation
5. Margin < MinMargin → skip
6. Calculate flip
7. Not profitable → skip
8. **Manipulation detected → skip** (needs ROI from step 6)
9. Score and add to candidates

**Tests:**
- Add `AnalyzeFlipsAsync_ManipulatedItem_FilteredOut` test to `FlipAnalyzerTests`
- Setup: valid item, profitable flip, but `_mockManipDetector.Returns(true)`
- Assert: empty result, scoring never called

**Config:** Already configured — `ManipulationSettings` in `appsettings.json` with
25% deviation threshold and high-ROI/low-volume check.

**Dependencies:** None.

---

## Fix 3 — Add Minimum Volume Filter

**Problem:** Items with <1,000 daily volume are effectively untradeable for flipping but
still appear in recommendations. The list gets polluted with illiquid items.

**Change:** Set `FlipSettings.MinVolume` default to `5000`.

**Files:**
- `Core/Entities/FlipSettings.cs` — change default value

**Changes:**
```csharp
public int MinVolume { get; set; } = 5_000; // was 0
```

**Tests:**
- Update `FlipSettingsTests.DefaultValues_MinVolume_EqualsZero` → `DefaultValues_MinVolume_Equals5000`
- Existing `FlipAnalyzerTests` already use `MinVolume = 10000` in `_settings`, so they pass unchanged

**Dependencies:** None. Interacts with Fix 4 conceptually (both relate to volume) but
the `MinVolume` filter uses `priceData.Volume24Hr` while Fix 4 changes fill time calculation.

---

## Fix 4 — Use Max Volume Across Windows for Fill Time

**Problem:** `Volume24Hr` from the 24h API endpoint sometimes reports *less* volume than
the 6h endpoint. The fill time calculation uses this undercounted value, producing
pessimistic fill time and GP/hour estimates.

**Change:** Compute the best volume estimate by extrapolating each window's volume to a
24h equivalent and using the maximum.

**Files:**
- `Core/Services/FlipCalculator.cs` — add `EstimateBestVolume24Hr` helper, use it for fill time

**Changes:**

Add helper method:
```csharp
private static int EstimateBestVolume24Hr(ItemPriceData data)
{
    var multipliers = new (TimeWindow Window, int Multiplier)[]
    {
        (TimeWindow.FiveMinute, 288),     // 24h / 5min = 288 periods
        (TimeWindow.OneHour, 24),
        (TimeWindow.SixHour, 4),
        (TimeWindow.TwentyFourHour, 1)
    };

    long maxVolume = 0;
    foreach (var (window, multiplier) in multipliers)
    {
        if (data.TimeWindows.TryGetValue(window, out var wp))
        {
            var extrapolated = (long)wp.TotalVolume * multiplier;
            if (extrapolated > maxVolume)
                maxVolume = extrapolated;
        }
    }

    return (int)Math.Min(maxVolume, int.MaxValue);
}
```

In `CalculateFlip`, change fill time to use best estimate:
```csharp
var volume24Hr = priceData.Volume24Hr;              // keep for display
var effectiveVolume = EstimateBestVolume24Hr(priceData); // use for fill time
var fillHours = _profitCalcService.CalculateEstimatedFillHours(
    buyLimit, quantity, effectiveVolume, settings.BuyLimitCycleHours);
```

`FlipCandidate.Volume24Hr` still shows the raw 24h window value for display.

**Tests:**
- Add `CalculateFlip_UsesMaxVolumeAcrossWindows_ForFillTime`:
  - 24h window: 10,000 volume, 1h window: 2,000 volume (extrapolated = 48,000)
  - Verify `CalculateEstimatedFillHours` is called with 48,000 not 10,000
  - Verify `candidate.Volume24Hr` is still 10,000
- Add `CalculateFlip_24hVolumeHighest_Uses24hVolume`:
  - When 24h window has the highest extrapolated volume, verify it's used
- Existing tests use `CreatePriceData(volume)` which only creates a 24h window → `EstimateBestVolume24Hr` returns `volume * 1` → no change to existing test behavior

**Dependencies:** None.

---

## Fix 5 — Replace ProfitPerCycle with TotalProfit in Scoring

**Problem:** `ProfitPerCycle = ProfitPerUnit × BuyLimit` ignores the user's budget.
A Twisted Bow scores amazing but with 10M budget you can't even buy one. `TotalProfit =
ProfitPerUnit × Quantity` already accounts for the budget cap and is a more realistic
measure of actual earnings.

**Change:** Score on `TotalProfit` instead of `ProfitPerCycle`. Keep `ProfitPerCycle` on
`FlipCandidate` for display.

**Files:**
- `Core/Configuration/ScoringConfiguration.cs` — rename properties
- `Core/Interfaces/IScoringService.cs` — rename method
- `Core/Services/ScoringService.cs` — rename method, read new config
- `Web/appsettings.json` — rename and recalibrate breakpoints

**Changes:**

`ScoringConfiguration.cs`:
```csharp
public List<BreakpointEntry> TotalProfitBreakpoints { get; set; } = [];  // was ProfitPerCycleBreakpoints
public double TotalProfitWeight { get; set; } = 0.35;                    // was ProfitPerCycleWeight
```

`IScoringService.cs`:
```csharp
double ScoreTotalProfit(long totalProfit);  // was ScoreProfitPerCycle
```

`ScoringService.cs`:
```csharp
public double ScoreTotalProfit(long totalProfit) =>
    Interpolate(totalProfit, _config.TotalProfitBreakpoints);

// In CalculateFlipScore:
var totalProfitScore = ScoreTotalProfit(candidate.TotalProfit);
// ...
+ (totalProfitScore * _config.TotalProfitWeight)
```

`appsettings.json` — recalibrated breakpoints (TotalProfit is typically smaller than
ProfitPerCycle since Quantity ≤ BuyLimit due to budget):
```json
"TotalProfitBreakpoints": [
    { "Threshold": 10000, "Score": 0.1 },
    { "Threshold": 50000, "Score": 0.3 },
    { "Threshold": 200000, "Score": 0.6 },
    { "Threshold": 500000, "Score": 0.8 },
    { "Threshold": 2000000, "Score": 1.0 }
],
"TotalProfitWeight": 0.35
```

**Tests:**
- `ScoringServiceTests`: rename `ScoreProfitPerCycle_*` → `ScoreTotalProfit_*`, update
  config to use `TotalProfitBreakpoints`/`TotalProfitWeight`, update breakpoint values
- `ScoringConfigurationTests`: rename `ProfitPerCycleBreakpoints` → `TotalProfitBreakpoints`,
  `ProfitPerCycleWeight` → `TotalProfitWeight`
- `CalculateFlipScore_*` tests: add `TotalProfit` to test `FlipCandidate` objects
- `FlipAnalyzerTests`: no changes (mocks `IScoringService`)

**Dependencies:** None. `ProfitPerCycle` stays on `FlipCandidate` and in the UI.

---

## Implementation Sequence

All fixes are independent and can be done in any order. Recommended sequence:

```
Fix 1 (config only)        — trivial, immediate impact on price accuracy
Fix 3 (1-line default)     — trivial, immediately cleans up illiquid noise
Fix 2 (pipeline + test)    — small, critical safety improvement
Fix 4 (helper + tests)     — moderate, fixes volume undercount
Fix 5 (rename + recalibrate) — moderate, most file changes
```

---

## Token Estimates

| Fix | Complexity | Low | High |
|-----|-----------|-----|------|
| Fix 1 — Price weights | Trivial (config) | 10,000 | 15,000 |
| Fix 3 — Min volume | Trivial (1 line) | 10,000 | 15,000 |
| Fix 2 — Manipulation | Small | 15,000 | 25,000 |
| Fix 4 — Max volume | Medium | 25,000 | 40,000 |
| Fix 5 — TotalProfit scoring | Medium | 25,000 | 45,000 |
| **Total** | | **85,000** | **140,000** |
