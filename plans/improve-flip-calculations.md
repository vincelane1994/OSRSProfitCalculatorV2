# Improve Flip Calculations — Implementation Plan

**Goal:** Produce a more accurate and useful flipping table by fixing core calculation flaws
identified through cross-referencing community guides (GE Tracker, 07Flip, OSRS Wiki,
GE Margin) with the current implementation.

---

## Analysis: Identified Flaws

### Flaw 1 — GP/Hour Is Massively Overestimated on Fast-Filling Items

**Current code** (`ProfitCalculationService.cs`):
```csharp
var buyHours  = Math.Min(buyLimit  / hourlyVolume, buyLimitCycleHours);  // capped at 4h
var sellHours = Math.Min(quantity  / hourlyVolume, buyLimitCycleHours);  // capped at 4h
return buyHours + sellHours;
```

**Problem:** When `hourlyVolume` >> `buyLimit` (i.e. the item trades far more than your buy
limit each hour), `buyHours` approaches 0. Example: 1,000-limit item trading 200,000/day
(~8,333/hr). `buyHours = 1000/8333 = 0.12h`, `sellHours = 0.12h` → `fillHours = 0.24h`.
If `totalProfit = 500,000 GP`, then `GP/Hour = 500,000 / 0.24 ≈ 2,083,333 GP/hr`.

**Reality:** The GE buy limit resets every 4 hours. Even if the offer fills in 15 minutes,
you cannot restart buying for the same item for 4 hours. The *effective* earnings rate is
`profitPerCycle / 4h = 500,000 / 4 = 125,000 GP/hr` — 16× lower than calculated.

**Fix:** `fillHours` used for GP/hour must be `max(actualFillHours, BuyLimitCycleHours)` so
that items filling in under 4 hours are not artificially boosted.

---

### Flaw 2 — Confidence Uses a Hardcoded Window Count

**Current code** (`ScoringService.cs`):
```csharp
var confidence = CalculateConfidence(
    candidate.HasSufficientData ? 4 : 2,   // <-- hardcoded guess
    candidate.Volume24Hr);
```

**Problem:** The actual number of time windows with usable data is computed inside
`PriceRecommendationService` but never stored on `FlipCandidate`. The `HasSufficientData`
flag only tells us whether both buy and sell have 2+ windows — not the actual count. Passing
`4` for all "sufficient data" items regardless of whether they have 2 or 4 windows inflates
confidence for items with sparse data.

**Fix:**
- Store `BuyWindowsUsed` and `SellWindowsUsed` (int) on `FlipCandidate`, populated from
  `PriceRecommendation.BuyWindowsUsed` / `SellWindowsUsed`.
- Pass `Math.Min(candidate.BuyWindowsUsed, candidate.SellWindowsUsed)` to
  `CalculateConfidence()`.

---

### Flaw 3 — Confidence Ignores Price Volatility (Key Risk Signal)

**Current formula:**
```
Confidence = (windowsUsed / minWindows * 0.6) + (volume / minVolume * 0.4)
```

**Problem:** A highly volatile item (5m price swings 30% vs 6h average) can still score
high confidence if it has good volume and 4 data windows. Price volatility is a primary
risk indicator for flippers: volatile prices mean your recommended buy/sell prices may be
stale by the time your offer fills.

Community tools (e.g. 07Flip) include volatility as ~30% of confidence score.

**Fix:** Add a price stability sub-score and restructure the confidence formula:

```
Confidence =
  Volume sub-score       × 0.40
  Price stability score  × 0.35
  Window coverage score  × 0.25
```

**Price stability score** = based on how much the 5-minute price deviates from the 6-hour
average (lower deviation = higher stability score):
- 0–5% deviation:    1.0
- 5–15% deviation:   0.7
- 15–30% deviation:  0.4
- >30% deviation:    0.1

To support this, `FlipCandidate` needs a `PriceVolatilityPercent` field computed in
`FlipCalculator` (compare 5m avg prices vs 6h avg prices, take the larger of buy/sell deviation).

---

### Flaw 4 — Profit Per Cycle Missing from Score and Table

**Problem:** Experienced flippers' primary evaluation metric is:
```
Profit Per Cycle = ProfitPerUnit × BuyLimit
```

This is independent of capital investment and represents the maximum earnings per 4-hour window.
A 5,000 GP margin item with buy limit 8 yields **40,000 GP/cycle**.
A 500 GP margin item with buy limit 10,000 yields **5,000,000 GP/cycle**.

The current scoring gives 25% weight to raw margin (GP per item), which heavily favours
high-value items regardless of their buy limit ceiling. This produces misleading rankings.

**Fix:**
- Add `ProfitPerCycle` (long) to `FlipCandidate` = `ProfitPerUnit × BuyLimit`.
- Replace the margin sub-score with a profit-per-cycle sub-score in `ScoringService`.
- Add `ProfitPerCycleBreakpoints` to `ScoringConfiguration` and `appsettings.json`.
- Display `ProfitPerCycle` as a column in the flip table.

Suggested breakpoints for `ProfitPerCycle`:
```
50,000 GP    → 0.1
250,000 GP   → 0.3
1,000,000 GP → 0.6
3,000,000 GP → 0.8
8,000,000 GP → 1.0
```

---

### Flaw 5 — ROI Breakpoints Don't Reflect the 2% Tax Break-Even

**Current breakpoints:**
```
0.5%  → 0.1
2.0%  → 0.3
5.0%  → 0.6
15.0% → 1.0
```

**Problem:** At 2% ROI on a sell price, the GE tax (2% of sell price) almost exactly cancels
out the gross margin, leaving ~0 net profit. Yet the current table scores a 2% ROI item at
0.3 — making it appear like a reasonable flip. Any item with ROI below ~2.05% after gross
margin means `ProfitPerUnit ≈ 0` or negative once tax is applied.

The score should reflect that items near the tax break-even point are marginal, not moderate.

**Fix — Revised ROI breakpoints:**
```
0.0%  → 0.0  (at or below break-even — no value)
1.0%  → 0.05 (tiny profit after tax, hardly worth time)
3.0%  → 0.3  (clear profit after 2% tax)
5.0%  → 0.6
10.0% → 0.85
20.0% → 1.0
```

---

### Flaw 6 — Manipulation Detection Threshold is Too Permissive

**Current threshold:** `deviationThresholdPercent = 50.0` (flags items where 5m price
deviates >50% from 24h average).

**Problem:** The OSRS community considers a >10–15% short-term vs long-term deviation as a
warning sign. A 50% threshold only catches extreme, obvious manipulation — subtle pumps that
are still highly risky go undetected.

Additionally, the detector misses the **high-ROI + low-volume** pattern, which is a classic
manipulation setup: a group buys out the supply (suppressing sell volume) and creates an
artificial spread.

**Fix:**
- Lower the default `deviationThresholdPercent` from 50% to 25%.
- Add a new check: if `RoiPercent > 8.0 AND Volume24Hr < 5,000`, flag as suspicious
  (wide spread on a thin market = manipulation risk).
- Make both thresholds configurable in `appsettings.json` under a `ManipulationDetection`
  section.

---

### Flaw 7 — Score Weights Don't Reflect How Experienced Flippers Rank Items

**Current weights:**
```
Volume      30%
Margin      25%   ← raw GP per item (misleading without buy limit context)
ROI         20%
GP/Hour     25%   ← currently inflated (see Flaw 1)
```

**Proposed weights (after fixes):**
```
Volume            20%   (liquidity — still important, but less dominant)
Profit/Cycle      35%   (replaces margin — primary earnings potential per 4h window)
ROI               20%   (capital efficiency — important for smaller banks)
GP/Hour           25%   (holistic profitability — will be accurate after Flaw 1 fix)
```

---

## Implementation Plan

The fixes are ordered by dependency. Each task is a standalone change that can be reviewed
independently.

---

### Task 1 — Store Actual Window Counts on FlipCandidate

**Files:**
- `Core/Entities/FlipCandidate.cs`
- `Core/ValueObjects/PriceRecommendation.cs` (check if WindowsUsed is available)
- `Core/Services/FlipCalculator.cs`

**Changes:**
1. Add `int BuyWindowsUsed { get; init; }` to `FlipCandidate`.
2. Add `int SellWindowsUsed { get; init; }` to `FlipCandidate`.
3. In `FlipCalculator.CalculateFlip()`, populate `BuyWindowsUsed = prices.BuyWindowsUsed`
   and `SellWindowsUsed = prices.SellWindowsUsed`.

**Tests:** Update `FlipCalculatorTests` to assert `BuyWindowsUsed` / `SellWindowsUsed`
are populated correctly from the `PriceRecommendation`.

**Token estimate:** 10,000 – 15,000 tokens

---

### Task 2 — Add PriceVolatilityPercent to FlipCandidate

**Files:**
- `Core/Entities/FlipCandidate.cs`
- `Core/Services/FlipCalculator.cs`
- `Core/Interfaces/IFlipCalculator.cs` (signature unchanged, but needs price data access)

**Problem:** `FlipCalculator.CalculateFlip()` currently receives a `PriceRecommendation`
(weighted averages only), not the raw `ItemPriceData`. Volatility requires comparing 5m vs
6h raw prices.

**Approach:**
- Add an overload or modify the signature to also accept `ItemPriceData priceData`.
- Compute `PriceVolatilityPercent` as:
  ```csharp
  static double ComputeVolatility(ItemPriceData data)
  {
      var has5m  = data.TimeWindows.TryGetValue(TimeWindow.FiveMinute, out var w5m);
      var has6h  = data.TimeWindows.TryGetValue(TimeWindow.SixHour,    out var w6h);
      if (!has5m || !has6h) return 0;

      double buyDev = 0, sellDev = 0;
      if (w5m.AvgBuyPrice > 0 && w6h.AvgBuyPrice > 0)
          buyDev = Math.Abs((double)(w5m.AvgBuyPrice.Value - w6h.AvgBuyPrice.Value)
                            / w6h.AvgBuyPrice.Value * 100.0);
      if (w5m.AvgSellPrice > 0 && w6h.AvgSellPrice > 0)
          sellDev = Math.Abs((double)(w5m.AvgSellPrice.Value - w6h.AvgSellPrice.Value)
                             / w6h.AvgSellPrice.Value * 100.0);
      return Math.Max(buyDev, sellDev);
  }
  ```
- Add `double PriceVolatilityPercent { get; init; }` to `FlipCandidate`.

**Note:** Check whether `FlipAnalyzer` passes `ItemPriceData` to the calculator; if not,
thread it through from `FlipAnalyzer` → `FlipCalculator`.

**Tests:** Unit tests for volatility computation with various 5m/6h combinations.

**Token estimate:** 15,000 – 25,000 tokens

---

### Task 3 — Add ProfitPerCycle to FlipCandidate and Score

**Files:**
- `Core/Entities/FlipCandidate.cs`
- `Core/Services/FlipCalculator.cs`
- `Core/Configuration/ScoringConfiguration.cs`
- `Core/Services/ScoringService.cs`
- `Core/Interfaces/IScoringService.cs`
- `Web/appsettings.json`

**Changes:**
1. Add `long ProfitPerCycle { get; init; }` to `FlipCandidate` = `ProfitPerUnit * BuyLimit`.
2. Populate it in `FlipCalculator.CalculateFlip()`.
3. Add `List<BreakpointEntry> ProfitPerCycleBreakpoints` and `double ProfitPerCycleWeight`
   to `ScoringConfiguration`.
4. Add `double ScoreProfitPerCycle(long profitPerCycle)` to `IScoringService` and
   `ScoringService`.
5. In `CalculateFlipScore`:
   - Remove `marginScore * MarginWeight`.
   - Add `profitPerCycleScore * ProfitPerCycleWeight`.
6. Remove `MarginBreakpoints` and `MarginWeight` from `ScoringConfiguration` (or keep for
   display/filter purposes only — confirm with user).
7. Update `appsettings.json`:
   ```json
   "ProfitPerCycleBreakpoints": [
     { "Threshold":   50000, "Score": 0.1 },
     { "Threshold":  250000, "Score": 0.3 },
     { "Threshold": 1000000, "Score": 0.6 },
     { "Threshold": 3000000, "Score": 0.8 },
     { "Threshold": 8000000, "Score": 1.0 }
   ],
   "ProfitPerCycleWeight": 0.35,
   "VolumeWeight": 0.20,
   "RoiWeight": 0.20,
   "GpPerHourWeight": 0.25
   ```

**Tests:** Update `ScoringServiceTests` for new weights and breakpoints.

**Token estimate:** 25,000 – 40,000 tokens

---

### Task 4 — Fix GP/Hour Overestimation (Buy Limit Cycle Floor)

**Files:**
- `Core/Services/ProfitCalculationService.cs`
- `Core/Interfaces/IProfitCalculationService.cs`

**Change:**
```csharp
public double CalculateEstimatedFillHours(
    int buyLimit, int quantity, int volume24Hr, double buyLimitCycleHours)
{
    var hourlyVolume = Math.Max(volume24Hr / 24.0, 1.0);
    var buyHours  = (double)buyLimit  / hourlyVolume;
    var sellHours = (double)quantity  / hourlyVolume;
    var rawFillHours = buyHours + sellHours;

    // The buy limit resets every buyLimitCycleHours (default 4h).
    // Even if offers fill faster, we cannot restart the buy side until the
    // cycle resets — so effective throughput is capped by one cycle per 4h.
    return Math.Max(rawFillHours, buyLimitCycleHours);
}
```

This change removes the per-phase `Math.Min(..., cycleHours)` caps and instead applies a
single floor at the cycle duration on the combined fill time. Items that fill fast will now
correctly report `GP/Hour ≈ profitPerCycle / 4h` rather than an inflated figure.

**Impact on tests:** `ProfitCalculationServiceTests.CalculateEstimatedFillHours_*` will need
updating. Expect significant GP/hour decreases for high-volume items — this is correct.

**Token estimate:** 15,000 – 25,000 tokens

---

### Task 5 — Revise Confidence Formula

**Files:**
- `Core/Services/ScoringService.cs`
- `Core/Interfaces/IScoringService.cs`
- `Core/Configuration/ScoringConfiguration.cs`
- `Web/appsettings.json`

**Changes:**

1. Add `double CalculateConfidence(int windowsUsed, int volume24Hr, double volatilityPercent)`
   overload (or replace the existing signature — check all callers).

2. New formula:
   ```csharp
   public double CalculateConfidence(int windowsUsed, int volume24Hr, double volatilityPercent)
   {
       // Volume score (40%)
       var volumeScore = Math.Min(volume24Hr / (double)_config.MinVolumeForHighConfidence, 1.0);

       // Price stability score (35%)
       var stabilityScore = volatilityPercent switch
       {
           <= 5  => 1.0,
           <= 15 => 0.7,
           <= 30 => 0.4,
           _     => 0.1
       };

       // Window coverage score (25%)
       var windowScore = Math.Min(windowsUsed / (double)_config.MinWindowsForHighConfidence, 1.0);

       return Math.Round(
           Math.Min(volumeScore * 0.40 + stabilityScore * 0.35 + windowScore * 0.25, 1.0), 2);
   }
   ```

3. Update `CalculateFlipScore` to call the new overload:
   ```csharp
   var confidence = CalculateConfidence(
       Math.Min(candidate.BuyWindowsUsed, candidate.SellWindowsUsed),
       candidate.Volume24Hr,
       candidate.PriceVolatilityPercent);
   ```

4. Add volatility thresholds to `ScoringConfiguration` (or keep them hardcoded as above).

**Tests:** Update `ScoringServiceTests` with volatility scenarios.

**Token estimate:** 20,000 – 35,000 tokens

---

### Task 6 — Fix ROI Breakpoints and Score Weights

**Files:**
- `Web/appsettings.json`

**Changes** (config-only, no code change):
```json
"RoiBreakpoints": [
  { "Threshold":  0.0, "Score": 0.00 },
  { "Threshold":  1.0, "Score": 0.05 },
  { "Threshold":  3.0, "Score": 0.30 },
  { "Threshold":  5.0, "Score": 0.60 },
  { "Threshold": 10.0, "Score": 0.85 },
  { "Threshold": 20.0, "Score": 1.00 }
],
```

Note: The `Interpolate()` function returns the first breakpoint score for values at or below
the minimum threshold. Adding a `0.0 → 0.00` entry ensures items at the tax break-even
receive a zero ROI score.

**Tests:** Verify no code changes required; add `ScoringServiceTests` for the new
breakpoint curve (2% ROI → score near 0.05, not 0.3).

**Token estimate:** 10,000 – 15,000 tokens

---

### Task 7 — Improve Manipulation Detection

**Files:**
- `Core/Services/ManipulationDetector.cs`
- `Core/Interfaces/IManipulationDetector.cs`
- `Core/Configuration/ManipulationSettings.cs` (new)
- `Web/appsettings.json`

**Changes:**

1. Create `ManipulationSettings.cs`:
   ```csharp
   public class ManipulationSettings
   {
       public double PriceDeviationThresholdPercent { get; set; } = 25.0;
       public double VolumeRatioThreshold            { get; set; } = 10.0;
       public double HighRoiThresholdPercent          { get; set; } = 8.0;
       public int    LowVolumeThreshold               { get; set; } = 5_000;
   }
   ```

2. Inject `IOptions<ManipulationSettings>` into `ManipulationDetector`.

3. Lower `deviationThresholdPercent` default to 25%.

4. Add new `HasHighRoiLowVolume(ItemPriceData, double roi)` check:
   ```csharp
   private bool HasHighRoiLowVolume(ItemPriceData priceData, double roiPercent)
   {
       if (!priceData.TimeWindows.TryGetValue(TimeWindow.TwentyFourHour, out var w24h))
           return false;
       var volume = (w24h.BuyVolume ?? 0) + (w24h.SellVolume ?? 0);
       return roiPercent > _settings.HighRoiThresholdPercent
           && volume < _settings.LowVolumeThreshold;
   }
   ```

5. Update `IsSuspicious` to accept and use `roi` parameter.

6. Update `FlipAnalyzer` to pass `candidate.RoiPercent` when calling `IsSuspicious`.

7. Register `ManipulationSettings` in DI.

**Tests:** Add manipulation detector tests for the new high-ROI/low-volume case and
lowered deviation threshold.

**Token estimate:** 25,000 – 40,000 tokens (cross-layer change)

---

### Task 8 — Expose ProfitPerCycle in the Flip Table

**Files:**
- `Web/ViewModels/FlipViewModel.cs` (or equivalent view model)
- `Web/Views/Flip/Index.cshtml` (or equivalent view)
- Any mapping code between `FlipCandidate` and the view model

**Changes:**
- Add `ProfitPerCycle` to the flip table view model.
- Add a `Profit/Cycle` column to the HTML table.
- Format it as `{value:N0} GP` (e.g. "1,250,000 GP").
- Make it sortable if the table supports column sorting.

**Token estimate:** 15,000 – 25,000 tokens

---

## Summary of Changes by Layer

| Layer        | Task | Change |
|---|---|---|
| Core/Entities | T1, T2, T3 | Add `BuyWindowsUsed`, `SellWindowsUsed`, `PriceVolatilityPercent`, `ProfitPerCycle` to `FlipCandidate` |
| Core/Services | T1 | `FlipCalculator`: populate new fields |
| Core/Services | T2 | `FlipCalculator`: compute volatility from raw price data |
| Core/Services | T3 | `FlipCalculator`: compute `ProfitPerCycle` |
| Core/Services | T4 | `ProfitCalculationService`: fix `CalculateEstimatedFillHours` buy-limit floor |
| Core/Services | T5 | `ScoringService`: revised confidence formula |
| Core/Services | T3, T5 | `ScoringService`: replace margin weight with profit-per-cycle weight |
| Core/Services | T7 | `ManipulationDetector`: lower threshold + high-ROI/low-volume check |
| Core/Config   | T3, T5, T7 | New/updated config POCOs |
| Infrastructure | T2 | Thread `ItemPriceData` through to `FlipCalculator` if not already available |
| Web/Config    | T3, T6 | `appsettings.json`: new breakpoints, updated weights |
| Web/Views     | T8 | Add `Profit/Cycle` column to flip table |

---

## Recommended Task Order

```
T1 (window counts)        — prerequisite for T5
T2 (volatility)           — prerequisite for T5
T4 (GP/hour fix)          — independent, high impact
T3 (profit per cycle)     — depends on T1 only
T5 (confidence formula)   — depends on T1 + T2 + T3
T6 (ROI breakpoints)      — independent (config only)
T7 (manipulation)         — independent
T8 (UI table)             — depends on T3
```

---

## Expected Impact on Rankings

After all changes:
- **High-volume consumables** (runes, potions, food) will rank higher — large `BuyLimit ×
  ProfitPerUnit` products.
- **High-value rare items** (Twisted Bow, Armadyl Crossbow) will rank lower unless their
  volume and cycle profits justify the position.
- **Items filling in minutes** will show accurate GP/Hour instead of inflated figures.
- **Volatile items** (prices swinging >15% in 5m vs 6h) will have suppressed confidence and
  scores — reducing the chance of recommending a flip that is already moving against you.
- **Manipulation targets** (wide spread, thin volume) will be filtered out more aggressively.

---

## Token Estimates

| Task | Low | High |
|---|---|---|
| T1 — Store window counts        | 10,000  | 15,000  |
| T2 — Price volatility           | 15,000  | 25,000  |
| T3 — Profit per cycle + score   | 25,000  | 40,000  |
| T4 — GP/Hour fix                | 15,000  | 25,000  |
| T5 — Confidence formula         | 20,000  | 35,000  |
| T6 — ROI breakpoints (config)   | 10,000  | 15,000  |
| T7 — Manipulation detection     | 25,000  | 40,000  |
| T8 — UI table                   | 15,000  | 25,000  |
| **Total**                       | **135,000** | **220,000** |

Multipliers applied: T3, T5, T7 are reviewer-feedback-likely (1.5×); T2 and T7 are
cross-cutting (1.5×). Estimates above already incorporate these multipliers.
