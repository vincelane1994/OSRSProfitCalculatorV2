# Epic 5: Smithing Calculator (Cannonballs + Dart Tips)

## Overview

Add a Smithing profit calculator that evaluates cannonball and dart tip smithing profitability. Users can compare profit/loss for each bar type across both product categories via a tabbed interface with client-side filtering and sorting.

This epic follows the established patterns from HighAlchingService/Controller and FlippingService/Controller.

---

## OSRS Item Data Reference

### Bar IDs (Inputs)
| Bar          | Item ID |
|--------------|---------|
| Bronze bar   | 2349    |
| Iron bar     | 2351    |
| Steel bar    | 2353    |
| Mithril bar  | 2359    |
| Adamant bar  | 2361    |
| Rune bar     | 2363    |

### Cannonball (Output)
| Product     | Item ID | Output Per Bar |
|-------------|---------|----------------|
| Cannonball  | 2       | 4              |

**Note:** Cannonballs are only smithed from **steel bars** (ID 2353). Only 1 cannonball product exists. The task description says "6 types" but there is only one cannonball type (steel bar -> 4 cannonballs). The service should handle this correctly by only defining cannonballs from steel bars. If the task explicitly requires 6 bar types for cannonballs, confirm with the user -- but the OSRS game only supports steel bars for cannonballs.

**IMPORTANT CLARIFICATION:** Re-reading the task description, it says "Cannonballs: 4 per bar, 6 types" and "Dart tips: 10 per bar, 6 types". The "6 types" likely refers to the 6 bar types for dart tips, and the cannonball line may be listing output-per-bar alongside total bar types in the system. In OSRS, cannonballs can ONLY be made from steel bars. The implementation should define cannonballs as steel-bar-only. If the task owner intended otherwise, this should be flagged during implementation.

### Dart Tip IDs (Outputs)
| Dart Tip     | Item ID | Bar Used    | Bar ID | Output Per Bar | Members |
|--------------|---------|-------------|--------|----------------|---------|
| Bronze dart tip  | 819 | Bronze bar  | 2349   | 10             | Yes     |
| Iron dart tip    | 820 | Iron bar    | 2351   | 10             | Yes     |
| Steel dart tip   | 821 | Steel bar   | 2353   | 10             | Yes     |
| Mithril dart tip | 822 | Mithril bar | 2359   | 10             | Yes     |
| Adamant dart tip | 823 | Adamant bar | 2361   | 10             | Yes     |
| Rune dart tip    | 824 | Rune bar    | 2363   | 10             | Yes     |

All dart tip smithing requires completion of "The Tourist Trap" quest and is members-only.

---

## Task Breakdown

### Task 1: Create SmithingItem Entity + SmithingType Enum
**Monday ID:** 11244193971
**Branch:** `feature/epic5-task1-smithing-entity`
**Complexity:** Small (2 new files, simple entity + enum)

#### Files to Create
1. `src/OSRSTools.Core/Entities/SmithingType.cs`
2. `src/OSRSTools.Core/Entities/SmithingItem.cs`

#### Implementation Details

**SmithingType.cs:**
```csharp
namespace OSRSTools.Core.Entities;

/// <summary>
/// Types of smithing operations for profit calculation.
/// </summary>
public enum SmithingType
{
    Cannonball,
    DartTip
}
```

**SmithingItem.cs:**
Follow the `HighAlchItem` pattern -- plain class with `{ get; init; }` properties, computed `IsProfitable`.

Properties (all `{ get; init; }` except computed):
- `int ItemId` -- output item ID (e.g., cannonball=2, bronze dart tip=819)
- `string Name` -- display name (e.g., "Cannonball", "Bronze dart tip")
- `SmithingType Type` -- Cannonball or DartTip
- `bool Members` -- always true for dart tips; cannonballs can be F2P
- `string BarName` -- display name of the bar (e.g., "Steel bar")
- `int BarId` -- item ID of the bar used
- `int BarPrice` -- current GE price of the bar
- `int OutputPrice` -- current GE price of one output item
- `int OutputPerInput` -- 4 for cannonballs, 10 for dart tips
- `int ProfitPerUnit` -- profit per bar smelted: (OutputPrice * OutputPerInput) - BarPrice
- `long TotalProfit` -- ProfitPerUnit * some quantity factor (or just ProfitPerUnit for display)
- `int Volume24Hr` -- 24h trading volume of the output item
- `double RoiPercent` -- ROI percentage

Computed:
- `bool IsProfitable => ProfitPerUnit > 0;`

#### Notes
- Match the XML doc comment style from `HighAlchItem.cs`
- Use `string.Empty` default for `Name` and `BarName`
- No dependencies on other layers

#### Token Estimate
- Low: 10,000
- High: 15,000

---

### Task 2: Create ISmithingService Interface + SmithingService Implementation
**Monday ID:** 11244187656
**Branch:** `feature/epic5-task2-smithing-service`
**Complexity:** Medium (interface + service with recipe definitions + DI wiring)

#### Files to Create
1. `src/OSRSTools.Core/Interfaces/ISmithingService.cs`
2. `src/OSRSTools.Core/Services/SmithingService.cs`

#### Files to Modify
1. `src/OSRSTools.Web/Program.cs` -- add DI registration

#### Implementation Details

**ISmithingService.cs:**
```csharp
using OSRSTools.Core.Entities;

namespace OSRSTools.Core.Interfaces;

public interface ISmithingService
{
    Task<IEnumerable<SmithingItem>> GetCannonballProfitsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SmithingItem>> GetDartTipProfitsAsync(CancellationToken cancellationToken = default);
}
```

**SmithingService.cs:**
Follow the `HighAlchingService` pattern:
- Constructor injection: `IDataFetchService`, `IProfitCalculationService`, `IPriceRecommendationService`, `ILogger<SmithingService>`
- Define static recipe data as private records or tuples containing (outputItemId, outputName, barId, barName, outputPerInput, members, smithingType)
- Cannonball recipe: (2, "Cannonball", 2353, "Steel bar", 4, false, SmithingType.Cannonball)
- Dart tip recipes: 6 entries for bronze through rune (IDs 819-824, all members=true)

**Core logic for each recipe:**
1. Fetch mappings + prices via `_dataFetchService.GetCompletePriceDataAsync()`
2. For each recipe, look up bar price and output price from the price data
3. Use `_priceRecommendationService.CalculateRecommendedPrices()` for both bar and output item to get recommended buy/sell prices
4. Use `_profitCalcService.CalculateMultiOutputProfit(barPrice, outputPrice, outputPerInput, volume)` to get profit calculation
5. Exclude items where the output has zero 24h volume
6. Build `SmithingItem` from the results
7. Return sorted by ProfitPerUnit descending

**Private helper:** A shared `CalculateSmithingProfits` method that takes a list of recipes and returns `IEnumerable<SmithingItem>`, called by both public methods.

**DI Registration in Program.cs:**
```csharp
builder.Services.AddScoped<ISmithingService, SmithingService>();
```
Add after the existing `IHighAlchingService` registration line.

#### Pitfalls
- Bar price should use `RecommendedBuyPrice` (we are buying bars)
- Output price should use `RecommendedSellPrice` (we are selling the output) -- OR use `RecommendedBuyPrice` of the output as a conservative sell estimate. Follow the same approach as HighAlchingService which uses `RecommendedBuyPrice` for input cost.
- Must handle cases where bar or output item has no price data (skip that recipe)
- Volume24Hr comes from the OUTPUT item's price data, not the bar's
- Cannonballs: Only steel bar (2353) -> cannonball (2). Do NOT create cannonball recipes for all 6 bar types.

#### Token Estimate
- Low: 25,000
- High: 40,000

---

### Task 3: Create SmithingController + Razor View with Tabs
**Monday ID:** 11244199100
**Branch:** `feature/epic5-task3-smithing-view`
**Complexity:** Medium-Large (controller + view model + Razor view with tabs + JS filter file)
**Multiplier:** Cross-cutting (touches Web layer across 4+ files) = 1.5x

#### Files to Create
1. `src/OSRSTools.Web/Controllers/SmithingController.cs`
2. `src/OSRSTools.Web/ViewModels/SmithingViewModel.cs`
3. `src/OSRSTools.Web/Views/Smithing/Index.cshtml`
4. `src/OSRSTools.Web/wwwroot/js/smithing-filter.js`

#### Files to Modify
- None -- the sidebar nav link for Smithing already exists in `_Layout.cshtml` (line 60-63)

#### Implementation Details

**SmithingViewModel.cs:**
Follow `HighAlchViewModel` / `FlippingViewModel` pattern:
```csharp
using OSRSTools.Core.Entities;

namespace OSRSTools.Web.ViewModels;

public class SmithingViewModel
{
    public List<SmithingItem> Cannonballs { get; set; } = new();
    public List<SmithingItem> DartTips { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int TotalCannonballs => Cannonballs.Count;
    public int TotalDartTips => DartTips.Count;
    public int ProfitableCannonballs => Cannonballs.Count(x => x.IsProfitable);
    public int ProfitableDartTips => DartTips.Count(x => x.IsProfitable);
}
```

**SmithingController.cs:**
Follow `HighAlchingController` pattern exactly:
- Constructor: inject `ISmithingService`
- `Index()` action: call both `GetCannonballProfitsAsync()` and `GetDartTipProfitsAsync()` concurrently with `Task.WhenAll`
- Wrap in try-catch, set `ErrorMessage` on failure
- Set `ViewData["LastSync"]`

```csharp
public async Task<IActionResult> Index()
{
    try
    {
        var cannonballsTask = _smithingService.GetCannonballProfitsAsync();
        var dartTipsTask = _smithingService.GetDartTipProfitsAsync();
        await Task.WhenAll(cannonballsTask, dartTipsTask);

        ViewData["LastSync"] = DateTime.UtcNow.ToString("h:mm tt") + " UTC";

        var viewModel = new SmithingViewModel
        {
            Cannonballs = (await cannonballsTask).ToList(),
            DartTips = (await dartTipsTask).ToList()
        };
        return View(viewModel);
    }
    catch (Exception ex)
    {
        var viewModel = new SmithingViewModel
        {
            ErrorMessage = "Failed to load Smithing data. Please try again later."
        };
        ViewBag.Error = ex.Message;
        return View(viewModel);
    }
}
```

**Index.cshtml:**
- Model: `@model OSRSTools.Web.ViewModels.SmithingViewModel`
- Title: "Smithing"
- Error alert (same pattern as HighAlching)
- Summary cards row: Total items, Profitable items, Showing count
- Bootstrap 5 tab nav (`nav-tabs`) with two tabs: "Cannonballs" and "Dart Tips"
- Each tab contains a sortable data table with columns: Bar, Output, Bar Price, Output Price, Profit, ROI%, Volume
- Serialize both lists to JS using `System.Text.Json` with `CamelCase` policy (per CLAUDE.md pitfall #7)
- Reference `smithing-filter.js`

**smithing-filter.js:**
Follow `highalch-filter.js` pattern but adapted for two tabs:
- Maintain separate `currentSort` state per tab (or a single sort that resets on tab switch)
- `applyFilters()` reads the active tab and filters/sorts the appropriate dataset
- Filter controls: Min Profit, Members (for dart tips), Reset
- `renderTable()` builds rows with: bar name, output name, bar price (gp), output price (gp), profit (colored), ROI%, volume
- Listen for Bootstrap tab shown event to re-apply filters
- `escapeHtml`, `formatGp`, `formatNumber` helper functions (same as highalch-filter.js)

#### Pitfalls
- JSON serialization: MUST use `JsonNamingPolicy.CamelCase` option (CLAUDE.md pitfall #7)
- Sidebar link already exists at line 60-63 of `_Layout.cshtml` -- do NOT add a duplicate
- The `Views/Smithing/` directory needs to be created
- Bootstrap tab switching requires listening to `shown.bs.tab` event to re-render

#### Token Estimate
- Base: 35,000
- With 1.5x cross-cutting multiplier: 52,500
- Low: 40,000
- High: 55,000

---

### Task 4: Write SmithingService Unit Tests
**Monday ID:** 11244200877
**Branch:** `feature/epic5-task4-smithing-service-tests`
**Complexity:** Medium (test file mirroring HighAlchingServiceTests pattern, minimum 6 tests)

#### Files to Create
1. `tests/OSRSTools.UnitTests/Core/Services/SmithingServiceTests.cs`

#### Implementation Details

Follow `HighAlchingServiceTests.cs` pattern exactly:
- Mock `IDataFetchService`, `IProfitCalculationService`, `IPriceRecommendationService`, `ILogger<SmithingService>`
- Create helper methods: `CreatePrices()`, `SetupRecommendedPrice()`
- No need for `CreateMappings()` since SmithingService uses hardcoded recipes, not mappings

**Required Tests (minimum 6):**

1. **`GetCannonballProfitsAsync_ValidPrices_Returns4OutputPerBar`**
   - Setup: steel bar price and cannonball price in price data
   - Assert: result has cannonball with OutputPerInput=4, correct ProfitPerUnit
   - Verify `CalculateMultiOutputProfit` called with outputPerInput=4

2. **`GetDartTipProfitsAsync_ValidPrices_Returns10OutputPerBar`**
   - Setup: at least one bar + dart tip price pair
   - Assert: result has dart tip with OutputPerInput=10
   - Verify `CalculateMultiOutputProfit` called with outputPerInput=10

3. **`GetDartTipProfitsAsync_AllItems_AreMembersOnly`**
   - Setup: all 6 bar/dart tip price pairs
   - Assert: every returned item has Members=true

4. **`GetCannonballProfitsAsync_ZeroVolume_Excluded`**
   - Setup: cannonball with Volume24Hr=0
   - Assert: empty result

5. **`GetDartTipProfitsAsync_AllSixBarTypes_Processed`**
   - Setup: all 6 bar types and corresponding dart tip prices
   - Assert: 6 results returned, verify each bar ID is correct (2349, 2351, 2353, 2359, 2361, 2363)

6. **`GetCannonballProfitsAsync_CorrectBarIds_Used`**
   - Verify that cannonball uses steel bar (2353) specifically
   - Assert bar ID and bar name are correct

7. **`GetDartTipProfitsAsync_NoPriceData_ReturnsEmpty`** (bonus)
   - Setup: empty price data
   - Assert: empty result

#### Test Data Pitfalls
- Must include `TimeWindow.TwentyFourHour` in test price data (CLAUDE.md pitfall #4)
- Price data needs entries for BOTH the bar AND the output item
- Mock `IPriceRecommendationService.CalculateRecommendedPrices()` for each item ID

#### Token Estimate
- Low: 25,000
- High: 45,000

---

### Task 5: Write SmithingController Unit Tests
**Monday ID:** 11244199070
**Branch:** `feature/epic5-task5-smithing-controller-tests`
**Complexity:** Small (test file mirroring HighAlchingControllerTests pattern, minimum 3 tests)

#### Files to Create
1. `tests/OSRSTools.UnitTests/Web/Controllers/SmithingControllerTests.cs`

#### Implementation Details

Follow `HighAlchingControllerTests.cs` pattern exactly:
- Mock `ISmithingService`
- Constructor creates `SmithingController` with mock

**Required Tests (minimum 3):**

1. **`Index_WithItems_ReturnsViewResultWithSmithingViewModel`**
   - Setup: mock returns cannonballs and dart tips lists
   - Assert: ViewResult with SmithingViewModel, both lists populated, ErrorMessage is null

2. **`Index_ServiceThrows_ReturnsViewWithErrorMessage`**
   - Setup: mock throws `HttpRequestException`
   - Assert: ViewResult with SmithingViewModel, empty lists, ErrorMessage contains "Failed to load"

3. **`Index_EmptyResults_ReturnsViewWithEmptyLists`**
   - Setup: mock returns empty enumerables
   - Assert: ViewResult with SmithingViewModel, both lists empty, TotalCannonballs=0, TotalDartTips=0, ErrorMessage is null

#### Token Estimate
- Low: 15,000
- High: 25,000

---

## Dependency Graph

```
Task 1 (Entity)
    |
    v
Task 2 (Service) --> Task 4 (Service Tests)
    |
    v
Task 3 (Controller + View) --> Task 5 (Controller Tests)
```

Tasks 1 -> 2 -> 3 are sequential. Tasks 4 and 5 can be done after their respective dependencies.

Recommended execution order: 1 -> 2 -> 4 -> 3 -> 5

This order allows service tests (Task 4) to validate the service before building the UI (Task 3), catching logic bugs early.

---

## Token Estimates Summary

| Task | Description | Low | High |
|------|-------------|-----|------|
| 1 | SmithingItem entity + SmithingType enum | 10,000 | 15,000 |
| 2 | ISmithingService + SmithingService + DI | 25,000 | 40,000 |
| 3 | SmithingController + View + ViewModel + JS | 40,000 | 55,000 |
| 4 | SmithingService unit tests | 25,000 | 45,000 |
| 5 | SmithingController unit tests | 15,000 | 25,000 |
| **Total** | | **115,000** | **180,000** |

Note: Each task includes agent overhead (tester ~5k + code-reviewer ~5k per run). Task 3 has a 1.5x cross-cutting multiplier applied. One re-validation cycle (~12k) is budgeted into the high estimates for Tasks 2-4.

---

## Risks and Mitigations

1. **Cannonball recipe ambiguity:** The task description says "6 types" for cannonballs, but OSRS only allows cannonballs from steel bars. Clarify with the task owner. Mitigation: implement steel-bar-only and add a comment noting the design decision.

2. **Missing price data for dart tips:** Low-volume dart tips (bronze, iron) may have no 24h price data in the API. Mitigation: the zero-volume exclusion filter handles this gracefully -- items with no data simply won't appear.

3. **CalculateMultiOutputProfit usage:** Ensure the `maxQuantity` parameter passed to `CalculateMultiOutputProfit` is meaningful. For smithing, there is no buy limit on bars. Consider using Volume24Hr as the quantity proxy, or a fixed quantity of 1 for per-unit display. Follow the task description which shows `ProfitPerUnit` and `TotalProfit` as separate fields -- `TotalProfit` may use volume as the quantity factor.

4. **JSON serialization for two lists:** The view needs to serialize both `Cannonballs` and `DartTips` lists to JavaScript. Use separate `var cannonballs = ...` and `var dartTips = ...` script variables with the `CamelCase` JSON policy.

---

## TODO Checklist

- [ ] Task 1: Create `SmithingType.cs` enum
- [ ] Task 1: Create `SmithingItem.cs` entity
- [ ] Task 2: Create `ISmithingService.cs` interface
- [ ] Task 2: Create `SmithingService.cs` with cannonball + dart tip recipes
- [ ] Task 2: Register `ISmithingService` in `Program.cs`
- [ ] Task 3: Create `SmithingViewModel.cs`
- [ ] Task 3: Create `SmithingController.cs`
- [ ] Task 3: Create `Views/Smithing/Index.cshtml` with Bootstrap tabs
- [ ] Task 3: Create `wwwroot/js/smithing-filter.js`
- [ ] Task 4: Create `SmithingServiceTests.cs` with 6+ tests
- [ ] Task 5: Create `SmithingControllerTests.cs` with 3+ tests
- [ ] All tasks: Build succeeds with 0 errors
- [ ] All tasks: All tests pass
