# Herblore Profit Calculator — Implementation Plan

## Overview

Add a Herblore Profit Calculator page that evaluates profitability of herb-related operations: cleaning herbs, making unfinished potions, and making finished potions. Each row represents one herb with columns for each operation type, following the same architectural pattern as the Smithing calculator.

The sidebar already has a Herblore link (bi-flower1 icon) in `_Layout.cshtml` pointing to `Herblore/Index`.

---

## Item ID Reference

### Vial of Water
| Item | ID |
|---|---|
| Vial of water | 227 |

### Herbs (Grimy, Clean, Unfinished Potion)

| Herb | Grimy ID | Clean ID | Unfinished Potion ID |
|---|---|---|---|
| Guam leaf | 199 | 249 | 91 |
| Marrentill | 201 | 251 | 93 |
| Tarromin | 203 | 253 | 95 |
| Harralander | 205 | 255 | 97 |
| Ranarr weed | 207 | 257 | 99 |
| Toadflax | 3049 | 2998 | 3002 |
| Irit leaf | 209 | 259 | 101 |
| Avantoe | 211 | 261 | 103 |
| Kwuarm | 213 | 263 | 105 |
| Snapdragon | 3051 | 3000 | 3004 |
| Cadantine | 215 | 265 | 107 |
| Lantadyme | 2485 | 2481 | 2483 |
| Dwarf weed | 217 | 267 | 109 |
| Torstol | 219 | 269 | 111 |

### Finished Potions (Primary Potion per Herb)

Each herb has a "primary" finished potion. We pick the most commonly made/traded potion per herb.

| Herb | Potion Name | Potion ID (3-dose) | Secondary Ingredient | Secondary ID |
|---|---|---|---|---|
| Guam leaf | Attack potion(3) | 121 | Eye of newt | 221 |
| Marrentill | Antipoison(3) | 175 | Unicorn horn dust | 235 |
| Tarromin | Strength potion(3) | 115 | Limpwurt root | 225 |
| Harralander | Restore potion(3) | 127 | Red spiders' eggs | 223 |
| Ranarr weed | Prayer potion(3) | 139 | Snape grass | 231 |
| Toadflax | Saradomin brew(3) | 6687 | Crushed nest | 6693 |
| Irit leaf | Super attack(3) | 145 | Eye of newt | 221 |
| Avantoe | Super energy(3) | 3018 | Mort myre fungus | 2970 |
| Kwuarm | Super strength(3) | 157 | Limpwurt root | 225 |
| Snapdragon | Super restore(3) | 3026 | Red spiders' eggs | 223 |
| Cadantine | Super defence(3) | 163 | White berries | 239 |
| Lantadyme | Antifire potion(3) | 2454 | Dragon scale dust | 241 |
| Dwarf weed | Ranging potion(3) | 169 | Wine of zamorak | 245 |
| Torstol | Zamorak brew(3) | 189 | Jangerberries | 247 |

### Additional Potions (Secondary per herb, where applicable)

These are secondary potions that use the same herb but a different secondary. Include only the most traded alternative:

| Herb | Potion Name | Potion ID (3-dose) | Secondary Ingredient | Secondary ID |
|---|---|---|---|---|
| Harralander | Energy potion(3) | 3010 | Chocolate dust | 1975 |
| Harralander | Combat potion(3) | 9741 | Goat horn dust | 9736 |
| Harralander | Compost potion(3) | 6472 | Volcanic ash | 21622 |
| Ranarr weed | Defence potion(3) | 133 | White berries | 239 |
| Toadflax | Agility potion(3) | 3034 | Toad's legs | 2152 |
| Irit leaf | Superantipoison(3) | 181 | Unicorn horn dust | 235 |
| Avantoe | Fishing potion(3) | 151 | Snape grass | 231 |
| Kwuarm | Weapon poison | 187 | Dragon scale dust | 241 |
| Lantadyme | Magic potion(3) | 3042 | Potato cactus | 3138 |

**Design decision**: For the initial implementation, include only the **primary potion per herb** (the 14-row table above). The secondary potions can be added in a follow-up task to keep scope manageable. Each herb row will show 5 profit columns (one per operation type).

---

## Architecture

### Files to Create

| Layer | File | Purpose |
|---|---|---|
| Core | `src/OSRSTools.Core/Entities/HerbloreItem.cs` | Entity for one herb's profitability across all operations |
| Core | `src/OSRSTools.Core/Entities/HerbloreOperation.cs` | Enum: Cleaning, UnfinishedFromGrimy, UnfinishedFromClean, PotionFromGrimy, PotionFromClean |
| Core | `src/OSRSTools.Core/Interfaces/IHerbloreService.cs` | Service contract |
| Core | `src/OSRSTools.Core/Services/HerbloreService.cs` | Service implementation with hardcoded recipes |
| Web | `src/OSRSTools.Web/Controllers/HerbloreController.cs` | MVC controller |
| Web | `src/OSRSTools.Web/ViewModels/HerbloreViewModel.cs` | ViewModel |
| Web | `src/OSRSTools.Web/Views/Herblore/Index.cshtml` | Razor view with table |
| Web | `src/OSRSTools.Web/wwwroot/js/herblore-filter.js` | Client-side filtering/sorting |
| Tests | `tests/OSRSTools.UnitTests/Core/Services/HerbloreServiceTests.cs` | Unit tests |

### Files to Modify

| File | Change |
|---|---|
| `src/OSRSTools.Web/Program.cs` | Add `builder.Services.AddScoped<IHerbloreService, HerbloreService>();` |

---

## Entity Design

### HerbloreOperation Enum

```csharp
namespace OSRSTools.Core.Entities;

/// <summary>
/// Types of herblore operations that can be evaluated for profitability.
/// </summary>
public enum HerbloreOperation
{
    /// <summary>Grimy herb -> Clean herb. Profit = clean - grimy.</summary>
    Cleaning,

    /// <summary>Grimy herb + Vial of water -> Unfinished potion. Profit = unf - grimy - vial.</summary>
    UnfinishedFromGrimy,

    /// <summary>Clean herb + Vial of water -> Unfinished potion. Profit = unf - clean - vial.</summary>
    UnfinishedFromClean,

    /// <summary>Grimy herb + Vial of water + Secondary -> Finished potion. Profit = potion - grimy - vial - secondary.</summary>
    PotionFromGrimy,

    /// <summary>Clean herb + Vial of water + Secondary -> Finished potion. Profit = potion - clean - vial - secondary.</summary>
    PotionFromClean
}
```

### HerbloreItem Entity

```csharp
namespace OSRSTools.Core.Entities;

/// <summary>
/// Represents profitability data for a single herb across all herblore operations.
/// One instance per herb — contains profit calculations for each operation type.
/// </summary>
public class HerbloreItem
{
    /// <summary>Display name of the herb (e.g., "Ranarr weed").</summary>
    public string HerbName { get; init; } = string.Empty;

    /// <summary>Whether this herb requires members.</summary>
    public bool Members { get; init; }

    // --- Grimy herb ---
    public int GrimyHerbId { get; init; }
    public int GrimyHerbPrice { get; init; }

    // --- Clean herb ---
    public int CleanHerbId { get; init; }
    public int CleanHerbPrice { get; init; }

    // --- Unfinished potion ---
    public int UnfinishedPotionId { get; init; }
    public int UnfinishedPotionPrice { get; init; }

    // --- Vial of water ---
    public int VialOfWaterPrice { get; init; }

    // --- Finished potion ---
    public string PotionName { get; init; } = string.Empty;
    public int PotionId { get; init; }
    public int PotionPrice { get; init; }

    // --- Secondary ingredient ---
    public string SecondaryName { get; init; } = string.Empty;
    public int SecondaryId { get; init; }
    public int SecondaryPrice { get; init; }

    // --- Profit per operation ---
    /// <summary>Profit from cleaning: CleanSellPrice - GrimyBuyPrice</summary>
    public int CleaningProfit { get; init; }

    /// <summary>Profit from unfinished (grimy): UnfSellPrice - GrimyBuyPrice - VialBuyPrice</summary>
    public int UnfinishedFromGrimyProfit { get; init; }

    /// <summary>Profit from unfinished (clean): UnfSellPrice - CleanBuyPrice - VialBuyPrice</summary>
    public int UnfinishedFromCleanProfit { get; init; }

    /// <summary>Profit from potion (grimy): PotionSellPrice - GrimyBuyPrice - VialBuyPrice - SecondaryBuyPrice</summary>
    public int PotionFromGrimyProfit { get; init; }

    /// <summary>Profit from potion (clean): PotionSellPrice - CleanBuyPrice - VialBuyPrice - SecondaryBuyPrice</summary>
    public int PotionFromCleanProfit { get; init; }

    // --- Best operation ---
    /// <summary>The most profitable operation for this herb.</summary>
    public HerbloreOperation BestOperation { get; init; }

    /// <summary>Profit of the best operation.</summary>
    public int BestProfit { get; init; }

    // --- Volume data ---
    /// <summary>24h volume of the grimy herb (indicates market activity).</summary>
    public int GrimyVolume24Hr { get; init; }

    /// <summary>24h volume of the clean herb.</summary>
    public int CleanVolume24Hr { get; init; }

    /// <summary>24h volume of the unfinished potion.</summary>
    public int UnfinishedVolume24Hr { get; init; }

    /// <summary>24h volume of the finished potion.</summary>
    public int PotionVolume24Hr { get; init; }
}
```

### HerbloreRecipe (private record struct inside HerbloreService)

```csharp
private record struct HerbloreRecipe(
    string HerbName,
    int GrimyHerbId,
    int CleanHerbId,
    int UnfinishedPotionId,
    string PotionName,
    int PotionId,
    string SecondaryName,
    int SecondaryId,
    bool Members);
```

---

## Service Design

### IHerbloreService Interface

```csharp
namespace OSRSTools.Core.Interfaces;

public interface IHerbloreService
{
    /// <summary>
    /// Calculates herblore profitability for all herbs across all operation types.
    /// Returns items sorted by BestProfit descending.
    /// </summary>
    Task<IReadOnlyList<HerbloreItem>> GetHerbloreProfitsAsync(CancellationToken cancellationToken = default);
}
```

### HerbloreService Implementation

Key logic in `CalculateHerbloreProfitsAsync`:

1. Fetch prices via `IDataFetchService.GetCompletePriceDataAsync()`
2. Get vial of water price (ID 227) - buy price
3. For each recipe:
   a. Look up grimy, clean, unfinished, potion, secondary prices
   b. Use `IPriceRecommendationService` for recommended buy/sell prices
   c. Calculate 5 profit values:
      - **Cleaning**: `cleanSellPrice - grimyBuyPrice`
      - **Unf from Grimy**: `unfSellPrice - grimyBuyPrice - vialBuyPrice`
      - **Unf from Clean**: `unfSellPrice - cleanBuyPrice - vialBuyPrice`
      - **Potion from Grimy**: `potionSellPrice - grimyBuyPrice - vialBuyPrice - secondaryBuyPrice`
      - **Potion from Clean**: `potionSellPrice - cleanBuyPrice - vialBuyPrice - secondaryBuyPrice`
   d. Determine best operation (highest profit among the 5)
   e. Build `HerbloreItem`
4. Sort by `BestProfit` descending
5. Return results

**Important**: Use `CalculateSimpleProfit` from `IProfitCalculationService` for each operation, or calculate directly. Since each operation is 1:1 input:output (no multi-output like smithing), we can compute profit directly as price differences. The service should still use `IPriceRecommendationService` for recommended buy/sell prices.

**Handling missing data**: If a herb's grimy or clean price data is missing, skip that herb entirely. If only the potion or secondary price is missing, set those profit columns to 0 (or mark them as unavailable) but still include the cleaning and unfinished profits.

### Hardcoded Recipes Array

```csharp
private static readonly int VialOfWaterId = 227;

private static readonly HerbloreRecipe[] Recipes =
[
    new("Guam leaf",     199, 249, 91,   "Attack potion(3)",    121, "Eye of newt",        221, false),
    new("Marrentill",    201, 251, 93,   "Antipoison(3)",       175, "Unicorn horn dust",  235, false),
    new("Tarromin",      203, 253, 95,   "Strength potion(3)",  115, "Limpwurt root",      225, false),
    new("Harralander",   205, 255, 97,   "Restore potion(3)",   127, "Red spiders' eggs",  223, true),
    new("Ranarr weed",   207, 257, 99,   "Prayer potion(3)",    139, "Snape grass",        231, true),
    new("Toadflax",      3049, 2998, 3002, "Saradomin brew(3)", 6687, "Crushed nest",      6693, true),
    new("Irit leaf",     209, 259, 101,  "Super attack(3)",     145, "Eye of newt",        221, true),
    new("Avantoe",       211, 261, 103,  "Super energy(3)",     3018, "Mort myre fungus",  2970, true),
    new("Kwuarm",        213, 263, 105,  "Super strength(3)",   157, "Limpwurt root",      225, true),
    new("Snapdragon",    3051, 3000, 3004, "Super restore(3)",  3026, "Red spiders' eggs",  223, true),
    new("Cadantine",     215, 265, 107,  "Super defence(3)",    163, "White berries",      239, true),
    new("Lantadyme",     2485, 2481, 2483, "Antifire potion(3)", 2454, "Dragon scale dust", 241, true),
    new("Dwarf weed",    217, 267, 109,  "Ranging potion(3)",   169, "Wine of zamorak",    245, true),
    new("Torstol",       219, 269, 111,  "Zamorak brew(3)",     189, "Jangerberries",      247, true),
];
```

---

## Controller Design

### HerbloreController

```csharp
public class HerbloreController : Controller
{
    private readonly IHerbloreService _herbloreService;

    public HerbloreController(IHerbloreService herbloreService)
    {
        _herbloreService = herbloreService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var items = await _herbloreService.GetHerbloreProfitsAsync();
            ViewData["LastSync"] = DateTime.UtcNow.ToString("h:mm tt") + " UTC";

            var viewModel = new HerbloreViewModel
            {
                Items = items.ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            var viewModel = new HerbloreViewModel
            {
                ErrorMessage = "Failed to load Herblore data. Please try again later."
            };
            ViewBag.Error = ex.Message;
            return View(viewModel);
        }
    }
}
```

---

## ViewModel Design

### HerbloreViewModel

```csharp
public class HerbloreViewModel
{
    public List<HerbloreItem> Items { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int TotalItems => Items.Count;
    public int ProfitableItems => Items.Count(x => x.BestProfit > 0);
}
```

---

## View Design

### Index.cshtml

Single table (no tabs needed since all herbs are in one list). Columns:

| Herb | Grimy Price | Clean Price | Cleaning Profit | Unf (Grimy) Profit | Unf (Clean) Profit | Potion | Potion (Grimy) Profit | Potion (Clean) Profit | Best Operation | Best Profit |

**Filter panel** (matching Smithing pattern):
- Min Profit (best profit)
- Profitability (All / Profitable Only / Unprofitable Only)
- Members filter (All / Members / F2P)
- Reset button

**Data serialization**: Use `System.Text.Json.JsonSerializer.Serialize` with `JsonNamingPolicy.CamelCase` as per project conventions.

---

## JS Filter Design

### herblore-filter.js

Follow the smithing-filter.js pattern:
- `applyFilters()` — filters and sorts data, renders table
- `sortTable(field)` — toggles sort on column
- `resetFilters()` — clears all filter inputs
- `renderTable(data)` — builds HTML rows with profit coloring
- Reuse `formatGp()`, `formatNumber()`, `escapeHtml()` helpers

Sort state tracks current sort field and direction. Default sort: `bestProfit` descending.

Profit coloring: apply `profit-positive` / `profit-negative` / `profit-neutral` CSS classes to all 5 profit columns and the best profit column.

---

## DI Registration

Add to `Program.cs` after the existing `AddScoped<ISmithingService>` line:

```csharp
builder.Services.AddScoped<IHerbloreService, HerbloreService>();
```

---

## Test Plan

### HerbloreServiceTests

Mirror the SmithingServiceTests pattern with mock setup helpers.

#### Test Cases

**Happy Path:**
1. `GetHerbloreProfitsAsync_ValidPrices_CalculatesCleaningProfit` — Verify cleaning profit = cleanSellPrice - grimyBuyPrice
2. `GetHerbloreProfitsAsync_ValidPrices_CalculatesUnfinishedFromGrimyProfit` — Verify profit = unfSellPrice - grimyBuyPrice - vialBuyPrice
3. `GetHerbloreProfitsAsync_ValidPrices_CalculatesUnfinishedFromCleanProfit` — Verify profit = unfSellPrice - cleanBuyPrice - vialBuyPrice
4. `GetHerbloreProfitsAsync_ValidPrices_CalculatesPotionFromGrimyProfit` — Verify profit = potionSellPrice - grimyBuyPrice - vialBuyPrice - secondaryBuyPrice
5. `GetHerbloreProfitsAsync_ValidPrices_CalculatesPotionFromCleanProfit` — Verify profit = potionSellPrice - cleanBuyPrice - vialBuyPrice - secondaryBuyPrice
6. `GetHerbloreProfitsAsync_ValidPrices_DeterminesBestOperation` — Verify BestOperation and BestProfit are correct
7. `GetHerbloreProfitsAsync_AllRecipes_ProcessedAndReturned` — Verify all 14 herbs are returned when all price data exists

**Edge Cases / Missing Data:**
8. `GetHerbloreProfitsAsync_MissingGrimyPrice_HerbExcluded` — Herb skipped entirely if grimy price missing
9. `GetHerbloreProfitsAsync_MissingCleanPrice_HerbExcluded` — Herb skipped entirely if clean price missing
10. `GetHerbloreProfitsAsync_MissingUnfinishedPrice_UnfinishedProfitsZero` — Unfinished profit columns are 0, cleaning and potion profits still calculated
11. `GetHerbloreProfitsAsync_MissingPotionPrice_PotionProfitsZero` — Potion profit columns are 0, cleaning and unfinished profits still calculated
12. `GetHerbloreProfitsAsync_MissingSecondaryPrice_PotionProfitsZero` — Same as above
13. `GetHerbloreProfitsAsync_MissingVialPrice_OnlyCleaningProfitCalculated` — Unfinished and potion profits are 0
14. `GetHerbloreProfitsAsync_NoPriceData_ReturnsEmpty` — Empty dictionary returns empty list
15. `GetHerbloreProfitsAsync_ResultsSortedByBestProfitDescending` — Verify sort order

**Members Flag:**
16. `GetHerbloreProfitsAsync_GuamLeaf_MembersFalse` — F2P herbs marked correctly
17. `GetHerbloreProfitsAsync_RanarrWeed_MembersTrue` — Members herbs marked correctly

---

## Task Breakdown

### Task 1: Core Entity and Enum
**Files**: `HerbloreItem.cs`, `HerbloreOperation.cs`
**Estimate**: 10,000 - 15,000 tokens (trivial)

- [ ] Create `HerbloreOperation` enum in `src/OSRSTools.Core/Entities/`
- [ ] Create `HerbloreItem` entity class in `src/OSRSTools.Core/Entities/`

### Task 2: Service Interface and Implementation
**Files**: `IHerbloreService.cs`, `HerbloreService.cs`
**Estimate**: 40,000 - 65,000 tokens (large - new service with 14 recipes, complex profit calc logic, needs thorough testing)

- [ ] Create `IHerbloreService` interface in `src/OSRSTools.Core/Interfaces/`
- [ ] Create `HerbloreService` in `src/OSRSTools.Core/Services/`
- [ ] Implement `GetHerbloreProfitsAsync` with all 14 recipes
- [ ] Handle missing price data gracefully
- [ ] Calculate all 5 profit types per herb
- [ ] Determine best operation per herb
- [ ] Sort results by best profit descending
- [ ] Register in `Program.cs`

### Task 3: Unit Tests
**Files**: `HerbloreServiceTests.cs`
**Estimate**: 30,000 - 50,000 tokens (medium-large - 17 test cases with mock setup)

- [ ] Create test class with mock setup helpers
- [ ] Implement all 17 test cases listed above
- [ ] Verify all tests pass

### Task 4: View, ViewModel, Controller, and JS
**Files**: `HerbloreController.cs`, `HerbloreViewModel.cs`, `Index.cshtml`, `herblore-filter.js`
**Estimate**: 30,000 - 50,000 tokens (medium-large - view with 10+ columns, filter JS)

- [ ] Create `HerbloreViewModel`
- [ ] Create `HerbloreController` with try/catch error handling
- [ ] Create `Index.cshtml` with data table, filter panel, summary cards
- [ ] Create `herblore-filter.js` with filtering, sorting, rendering
- [ ] Serialize data with camelCase JSON naming policy
- [ ] Verify page loads and filters work

### Total Token Estimate

| Task | Low | High |
|---|---|---|
| Task 1: Entity + Enum | 10,000 | 15,000 |
| Task 2: Service | 40,000 | 65,000 |
| Task 3: Tests | 30,000 | 50,000 |
| Task 4: View Layer | 30,000 | 50,000 |
| **Total** | **110,000** | **180,000** |

*Multiplier note*: Tasks 2-3 are likely to need reviewer feedback (new pattern with 5 profit columns per herb, complex profit logic) so the high estimates already include a 1.3x buffer.

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Item IDs are wrong | Incorrect prices fetched | Cross-reference with OSRS Wiki API at runtime; items with no price data are safely skipped |
| Some herbs have zero GE volume | Empty/sparse results | Skip herbs with zero volume on key items (grimy/clean), show 0 for operations with zero-volume outputs |
| Too many API lookups per herb (5 items each) | Slow page load | All prices come from one `GetCompletePriceDataAsync()` call (cached); no per-herb API calls |
| Profit calculations differ from expected | User confusion | Document profit formulas clearly in tooltips/column headers |
| Weapon poison is not a 3-dose potion | Wrong ID lookup | Weapon poison (ID 187) is a single-dose item; excluded from initial implementation (Kwuarm's primary potion is Super strength) |

---

## Future Enhancements (Out of Scope)

1. **Secondary potions per herb** — Harralander has 4 potions, Irit has 2, etc. Add a dropdown or expandable rows.
2. **GP/XP column** — Show cost or profit per XP gained for each operation.
3. **Herblore level filter** — Filter by minimum Herblore level required.
4. **Grimy-to-potion pipeline** — Show total profit for the full chain (buy grimy -> clean -> make unf -> make potion).
