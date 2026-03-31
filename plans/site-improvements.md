# Site Improvements — Implementation Plan

**Goal:** 12 improvements to make the site more useful and more accurate, organized
into independent tasks that can be implemented in any order.

---

## Improvement 1 — Persist Filter Settings to localStorage

**Problem:** Every page load resets filters to defaults. Users must re-enter their
budget, preferred min volume, and members preference every time.

**Change:** Save filter values to `localStorage` on change, restore them on page load.

**Files:**
- `Web/wwwroot/js/flipping-filter.js`
- `Web/wwwroot/js/highalch-filter.js`
- `Web/wwwroot/js/smithing-filter.js`
- `Web/wwwroot/js/herblore-filter.js`

**Changes per JS file:**

Add two helper functions:
```javascript
function saveFilters(pageKey, filters) {
    localStorage.setItem('osrs_filters_' + pageKey, JSON.stringify(filters));
}

function loadFilters(pageKey) {
    try {
        var saved = localStorage.getItem('osrs_filters_' + pageKey);
        return saved ? JSON.parse(saved) : null;
    } catch (e) { return null; }
}
```

On `DOMContentLoaded`, before `applyFilters()`:
```javascript
var saved = loadFilters('flipping'); // or 'highalch', 'smithing', 'herblore'
if (saved) {
    // Set each input element value from saved object
    if (saved.members) document.getElementById('memberFilter').value = saved.members;
    if (saved.minMargin) document.getElementById('minMargin').value = saved.minMargin;
    // ... etc for each filter input
}
applyFilters();
```

In `applyFilters()`, after reading filter values, add:
```javascript
saveFilters('flipping', { members, minMargin, minVolume, minGpHr, minConfidence, maxFillTime });
```

In `resetFilters()`, add:
```javascript
localStorage.removeItem('osrs_filters_flipping');
```

**Filter keys per page:**
- `flipping`: members, minMargin, minVolume, minGpHr, minConfidence, maxFillTime
- `highalch`: members, minProfit, maxBuyPrice, minVolume, maxInvestment
- `smithing`: minProfit, minVolume, profitability
- `herblore`: minProfit, minVolume, profitability

**Tests:** Manual — verify filters persist across page reload and reset clears them.

**Dependencies:** None.

---

## Improvement 2 — Show Data Age Per Item

**Problem:** Users can't tell how stale the displayed prices are. An item last traded
30 minutes ago looks identical to one traded 2 seconds ago.

**Change:** Add a "Last Trade" column showing how recently the item was traded, using
the `/latest` endpoint timestamps already fetched and stored on `ItemPriceData`.

**Files:**
- `Core/Entities/FlipCandidate.cs` — add `LastTradeTime` field
- `Core/Services/FlipCalculator.cs` — populate `LastTradeTime` from `ItemPriceData`
- `Web/Views/Flipping/Index.cshtml` — add column header
- `Web/wwwroot/js/flipping-filter.js` — render column, add formatting helper

**Changes:**

`FlipCandidate.cs` — add field:
```csharp
/// <summary>Most recent trade timestamp (min of buy/sell latest times).</summary>
public DateTime? LastTradeTime { get; set; }
```

`FlipCalculator.cs` — in `CalculateFlip`, populate from `ItemPriceData`:
```csharp
// Pick the most recent of buy/sell timestamps
DateTime? lastTrade = null;
if (priceData.LatestBuyTime.HasValue && priceData.LatestSellTime.HasValue)
    lastTrade = priceData.LatestBuyTime > priceData.LatestSellTime
        ? priceData.LatestBuyTime : priceData.LatestSellTime;
else
    lastTrade = priceData.LatestBuyTime ?? priceData.LatestSellTime;
```

Set `LastTradeTime = lastTrade` in the returned `FlipCandidate`.

`flipping-filter.js` — add formatting helper:
```javascript
function formatAge(isoString) {
    if (!isoString) return '<span class="text-secondary">—</span>';
    var seconds = Math.floor((Date.now() - new Date(isoString).getTime()) / 1000);
    if (seconds < 60) return seconds + 's ago';
    if (seconds < 3600) return Math.floor(seconds / 60) + 'm ago';
    if (seconds < 86400) return Math.floor(seconds / 3600) + 'h ago';
    return Math.floor(seconds / 86400) + 'd ago';
}
```

Add a CSS class for stale items (>15 min):
```javascript
var ageClass = seconds > 900 ? 'profit-negative' : (seconds > 300 ? 'text-secondary' : 'profit-positive');
```

Add "Last Trade" column to table header and `renderTable()`.

**Note:** `LatestBuyTime` and `LatestSellTime` are already fetched from `/latest` and
stored on `ItemPriceData`. They are `DateTime?` fields. The `FlipCalculator.CalculateFlip`
method already receives `ItemPriceData priceData` — no new data fetching needed.

**Tests:**
- `FlipCalculatorTests`: Add test that `LastTradeTime` is populated from priceData
- `FlipCalculatorTests`: Add test that `LastTradeTime` is null when no latest timestamps

**Dependencies:** None.

---

## Improvement 3 — Buy/Sell Volume Imbalance Display

**Problem:** A 50k volume item where 48k is sells and 2k is buys has a very different
flip reality than a balanced 25k/25k split, but the site only shows total volume.

**Change:** Add a buy:sell ratio indicator to the flipping detail modal and an
imbalance flag on the table.

**Files:**
- `Web/wwwroot/js/flipping-filter.js` — add ratio display in modal and table

**Changes:**

The `WindowPriceSnapshot` already has `buyVolume` and `sellVolume` fields, and the
`showItemDetail()` modal already displays per-window volumes. The 24h window data
is available in the `windowPrices` array.

In `showItemDetail()`, add a "Volume Balance" stat to the Item Info section:
```javascript
var w24h = item.windowPrices.find(w => w.window === '24 hour');
if (w24h && w24h.buyVolume && w24h.sellVolume) {
    var total = w24h.buyVolume + w24h.sellVolume;
    var buyPct = Math.round(w24h.buyVolume / total * 100);
    var sellPct = 100 - buyPct;
    var balanceClass = Math.abs(buyPct - 50) > 25 ? 'profit-negative' : 'profit-positive';
    // Display: "Buy 30% / Sell 70%" with color coding
}
```

In `renderTable()`, add a small visual indicator next to Volume 24h:
```javascript
// If buy/sell data available from 24h window, show imbalance icon
var imbalanced = /* buyPct < 25 || buyPct > 75 */;
if (imbalanced) volumeCell += ' <span title="Volume imbalance">⚠</span>';
```

**Tests:** Manual — verify imbalance indicator appears correctly.

**Dependencies:** None. Uses existing `windowPrices` data already serialized to JS.

---

## Improvement 4 — Instant vs Recommended Price Gap

**Problem:** When the latest instant trade price diverges from the weighted
recommendation, the market is moving fast and the recommendation may be stale.
This gap is never shown.

**Change:** Add instant prices to the flipping detail modal and flag items where
the gap exceeds 5%.

**Files:**
- `Core/Entities/FlipCandidate.cs` — add `LatestBuyPrice`, `LatestSellPrice`
- `Core/Services/FlipCalculator.cs` — populate from `ItemPriceData`
- `Web/wwwroot/js/flipping-filter.js` — display in modal, add gap warning

**Changes:**

`FlipCandidate.cs`:
```csharp
/// <summary>Most recent instant-buy price from /latest endpoint.</summary>
public int? LatestBuyPrice { get; set; }

/// <summary>Most recent instant-sell price from /latest endpoint.</summary>
public int? LatestSellPrice { get; set; }
```

`FlipCalculator.cs` — populate:
```csharp
LatestBuyPrice = priceData.LatestBuyPrice,
LatestSellPrice = priceData.LatestSellPrice,
```

`flipping-filter.js` — in `showItemDetail()`, add to Pricing section:
```javascript
if (item.latestBuyPrice) {
    var buyGap = Math.abs(item.latestBuyPrice - item.recommendedBuyPrice) / item.recommendedBuyPrice * 100;
    // Show "Latest Buy: X,XXX gp (↑5.2% from rec.)" with color coding if >5%
}
// Same for latestSellPrice vs recommendedSellPrice
```

**Tests:**
- `FlipCalculatorTests`: Verify LatestBuyPrice/LatestSellPrice are populated from priceData

**Dependencies:** None. `LatestBuyPrice`/`LatestSellPrice` already exist on `ItemPriceData`.

---

## Improvement 5 — GP/Hr for High Alching

**Problem:** High Alch profit is shown per item but there's no GP/hr column. Items
with high profit but low buy limits (e.g. 8/4h) earn far less per hour than items
with lower profit but high buy limits (e.g. 10,000/4h). Without GP/hr, users
can't compare effectively.

**Change:** Add `GpPerHour` to `HighAlchItem` and display it in the table.

**Files:**
- `Core/Entities/HighAlchItem.cs` — add `GpPerHour` field
- `Core/Services/HighAlchingService.cs` — calculate GP/hr
- `Web/Views/HighAlching/Index.cshtml` — add column header
- `Web/wwwroot/js/highalch-filter.js` — render column, allow sorting

**Changes:**

`HighAlchItem.cs`:
```csharp
/// <summary>Estimated GP per hour based on buy limit and 4-hour cycle.</summary>
public double GpPerHour { get; set; }
```

`HighAlchingService.cs` — after calculating profit, add:
```csharp
// Items per hour = BuyLimit / 4.0 (4-hour buy limit cycle)
// High Alch rate ≈ 1,200/hour but limited by buy limit cycle
var itemsPerHour = Math.Min(1200, mapping.BuyLimit / 4.0);
item.GpPerHour = item.Profit > 0 ? item.Profit * itemsPerHour : 0;
```

Note: A player can high alch ~1,200 items/hour (one every 3 seconds). But they can
only *buy* `BuyLimit / 4` items per hour on the GE. The bottleneck is whichever is
lower.

`highalch-filter.js` — add column rendering and sorting support:
```javascript
// Add "GP/hr" column after ROI%
// Format with formatGpHr() helper (already exists in flipping-filter.js, copy it)
// Add sort support for 'gpPerHour' field
```

`Index.cshtml` — add `<th>` header for GP/hr.

**Tests:**
- `HighAlchingServiceTests`: Add test for GpPerHour calculation
- `HighAlchingServiceTests`: Add test that GpPerHour is 0 for unprofitable items

**Dependencies:** None.

---

## Improvement 6 — Output Volume for Smithing/Herblore

**Problem:** Smithing and Herblore calculators show input prices and profit but don't
show whether the output item actually has volume to sell. A "profitable" dart tip
with 0 sell volume is useless.

**Change:** Add output item volume to `SmithingItem` and `HerbloreItem`, display in
tables, and flag low-volume outputs.

**Files:**
- `Core/Entities/SmithingItem.cs` — add `OutputVolume24Hr`
- `Core/Entities/HerbloreItem.cs` — add `OutputVolume24Hr`
- `Core/Services/SmithingService.cs` — populate output volume from price data
- `Core/Services/HerbloreService.cs` — populate output volume from price data
- `Web/Views/Smithing/Index.cshtml` — add column
- `Web/Views/Herblore/Index.cshtml` — add column
- `Web/wwwroot/js/smithing-filter.js` — render column
- `Web/wwwroot/js/herblore-filter.js` — render column

**Changes:**

`SmithingItem.cs` and `HerbloreItem.cs`:
```csharp
/// <summary>24-hour volume of the output item.</summary>
public int OutputVolume24Hr { get; set; }
```

In the services, when building the item, look up the output item's volume from
the price data dictionary (already passed to these services via `IDataFetchService`):
```csharp
OutputVolume24Hr = prices.TryGetValue(outputItemId, out var outputPriceData)
    ? outputPriceData.Volume24Hr
    : 0
```

In the views and JS files, add an "Output Vol" column. Flag items where
`OutputVolume24Hr < 1000` with a warning style.

**Tests:**
- `SmithingServiceTests`: Verify OutputVolume24Hr is populated
- `HerbloreServiceTests`: Verify OutputVolume24Hr is populated

**Dependencies:** None. Price data for output items is already fetched by
`GetCompletePriceDataAsync()`.

---

## Improvement 7 — Item Search and Favorites

**Problem:** With hundreds of items per page, there's no way to quickly find a
specific item or bookmark items you regularly flip.

**Change:** Add a search box above each table and a favorites system using localStorage.

**Files:**
- `Web/Views/Flipping/Index.cshtml` — add search input and favorites filter toggle
- `Web/Views/HighAlching/Index.cshtml` — same
- `Web/wwwroot/js/flipping-filter.js` — add search and favorites logic
- `Web/wwwroot/js/highalch-filter.js` — add search and favorites logic
- `Web/wwwroot/css/site.css` — add star button styling

**Changes:**

Add search input above table (in each view):
```html
<div class="d-flex gap-2 mb-2">
    <input type="text" id="itemSearch" class="form-control form-control-sm"
           placeholder="Search items..." style="max-width: 300px;">
    <button id="favoritesToggle" class="btn btn-sm btn-outline-warning"
            title="Show favorites only">★</button>
</div>
```

JS — favorites helpers (shared pattern for each page):
```javascript
function getFavorites(pageKey) {
    try {
        return JSON.parse(localStorage.getItem('osrs_favorites_' + pageKey) || '[]');
    } catch (e) { return []; }
}

function toggleFavorite(pageKey, itemId) {
    var favs = getFavorites(pageKey);
    var idx = favs.indexOf(itemId);
    if (idx >= 0) favs.splice(idx, 1);
    else favs.push(itemId);
    localStorage.setItem('osrs_favorites_' + pageKey, JSON.stringify(favs));
    applyFilters();
}
```

In `applyFilters()`, add search filter:
```javascript
var searchTerm = document.getElementById('itemSearch').value.toLowerCase();
if (searchTerm) {
    filtered = filtered.filter(item => item.name.toLowerCase().includes(searchTerm));
}

// If favorites toggle is active
if (showFavoritesOnly) {
    var favs = getFavorites('flipping');
    filtered = filtered.filter(item => favs.includes(item.itemId));
}
```

In `renderTable()`, add a star column as the first column:
```javascript
var favs = getFavorites('flipping');
var isFav = favs.includes(item.itemId);
var starHtml = '<td class="text-center" style="cursor:pointer" '
    + 'onclick="event.stopPropagation(); toggleFavorite(\'flipping\', ' + item.itemId + ')">'
    + (isFav ? '★' : '☆') + '</td>';
```

Wire up the search input with a debounced `applyFilters()` call:
```javascript
document.getElementById('itemSearch').addEventListener('input', function() {
    clearTimeout(this._debounce);
    this._debounce = setTimeout(applyFilters, 200);
});
```

**Tests:** Manual — verify search filters as you type, favorites persist across reload.

**Dependencies:** None.

---

## Improvement 8 — Auto-Refresh

**Problem:** Prices update every 5 minutes but the page is static. Users must
manually reload to see new data.

**Change:** Add auto-refresh that fetches fresh data via AJAX without full page reload.

**Files:**
- `Web/Controllers/FlippingController.cs` — add JSON endpoint
- `Web/Controllers/HighAlchingController.cs` — add JSON endpoint
- `Web/Controllers/SmithingController.cs` — add JSON endpoint
- `Web/Controllers/HerbloreController.cs` — add JSON endpoint
- `Web/wwwroot/js/flipping-filter.js` — add refresh timer and fetch logic
- `Web/wwwroot/js/highalch-filter.js` — same
- `Web/wwwroot/js/smithing-filter.js` — same
- `Web/wwwroot/js/herblore-filter.js` — same
- `Web/wwwroot/css/site.css` — add countdown/refresh indicator styling

**Changes:**

Add a JSON endpoint to each controller:
```csharp
[HttpGet]
public async Task<IActionResult> Data()
{
    try
    {
        var items = await _flipAnalyzer.AnalyzeFlipsAsync(new FlipSettings());
        return Json(items, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to fetch data");
        return StatusCode(500);
    }
}
```

Add refresh UI to each page's filter panel:
```html
<div class="d-flex align-items-center gap-2">
    <label class="form-check-label small">Auto-refresh</label>
    <select id="refreshInterval" class="form-select form-select-sm" style="width:auto">
        <option value="0">Off</option>
        <option value="300" selected>5 min</option>
        <option value="600">10 min</option>
    </select>
    <span id="refreshCountdown" class="text-secondary small"></span>
    <button id="refreshNow" class="btn btn-sm btn-outline-secondary" title="Refresh now">↻</button>
</div>
```

JS — auto-refresh logic:
```javascript
var refreshTimer = null;
var countdownTimer = null;
var secondsLeft = 0;

function startAutoRefresh(intervalSeconds) {
    stopAutoRefresh();
    if (intervalSeconds <= 0) return;
    secondsLeft = intervalSeconds;
    countdownTimer = setInterval(function() {
        secondsLeft--;
        document.getElementById('refreshCountdown').textContent = secondsLeft + 's';
        if (secondsLeft <= 0) refreshData();
    }, 1000);
}

function stopAutoRefresh() {
    clearInterval(countdownTimer);
    document.getElementById('refreshCountdown').textContent = '';
}

function refreshData() {
    fetch('/Flipping/Data')
        .then(r => r.json())
        .then(data => {
            items = data;              // Replace global items array
            applyFilters();            // Re-apply current filters
            secondsLeft = parseInt(document.getElementById('refreshInterval').value);
        })
        .catch(err => console.error('Refresh failed:', err));
}

document.getElementById('refreshInterval').addEventListener('change', function() {
    startAutoRefresh(parseInt(this.value));
});

document.getElementById('refreshNow').addEventListener('click', refreshData);

// Start on page load
startAutoRefresh(300);
```

**Tests:**
- Controller tests: Add test for `Data()` endpoint returns JSON
- Manual: Verify auto-refresh updates table without losing filter/sort/page state

**Dependencies:** None.

---

## Improvement 9 — CSV Export

**Problem:** The "Export" link in the sidebar does nothing. Users can't export
filtered results for external analysis.

**Change:** Add client-side CSV export of the currently filtered and sorted table data.

**Files:**
- `Web/wwwroot/js/flipping-filter.js` — add export function
- `Web/wwwroot/js/highalch-filter.js` — add export function
- `Web/wwwroot/js/smithing-filter.js` — add export function
- `Web/wwwroot/js/herblore-filter.js` — add export function
- `Web/Views/Shared/_Layout.cshtml` — wire up Export sidebar link

**Changes:**

Add export function to each JS file (flipping example):
```javascript
function exportCsv() {
    var headers = ['Name','Buy','Sell','Margin','Tax','Profit/Unit','Qty',
                   'Total Profit','Profit/Cycle','ROI%','GP/hr','Confidence',
                   'Score','Volume 24h'];
    var rows = currentFiltered.map(item => [
        item.name, item.recommendedBuyPrice, item.recommendedSellPrice,
        item.margin, item.taxAmount, item.profitPerUnit, item.quantity,
        item.totalProfit, item.profitPerCycle, item.roiPercent,
        Math.round(item.gpPerHour), item.confidenceRating,
        item.flipScore, item.volume24Hr
    ]);
    var csv = [headers.join(',')]
        .concat(rows.map(r => r.map(v =>
            typeof v === 'string' ? '"' + v.replace(/"/g, '""') + '"' : v
        ).join(',')))
        .join('\n');
    var blob = new Blob([csv], { type: 'text/csv' });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = 'osrs-flipping-' + new Date().toISOString().slice(0,10) + '.csv';
    a.click();
    URL.revokeObjectURL(url);
}
```

Update the Export sidebar link in `_Layout.cshtml`:
```html
<a class="nav-link" href="#" onclick="if(typeof exportCsv==='function')exportCsv()">
    <i class="bi bi-download"></i><span class="nav-label">Export</span>
</a>
```

The export always exports the *currently filtered and sorted* data, not all data.

**Tests:** Manual — verify CSV downloads with correct data matching the filtered table.

**Dependencies:** None.

---

## Improvement 10 — Dashboard Mini-Tables

**Problem:** The carousel shows one item at a time and auto-rotates. Users can only
see 1 of 5 items and must wait or click through to see the rest.

**Change:** Replace carousels with compact mini-tables showing all top 5 items at once.

**Files:**
- `Web/Views/Home/Index.cshtml` — replace carousel HTML with mini-tables
- `Web/wwwroot/js/dashboard.js` — replace carousel JS with mini-table rendering

**Changes:**

Replace each carousel card with a mini-table:
```html
<div class="card mb-3">
    <div class="card-header d-flex justify-content-between">
        <span><i class="bi bi-graph-up"></i> Top Flips</span>
        <a href="/Flipping" class="small">View all →</a>
    </div>
    <div class="card-body p-0">
        <table class="table table-sm mb-0" id="topFlipsTable">
            <thead><tr>
                <th>Item</th><th>GP/hr</th><th>ROI%</th>
            </tr></thead>
            <tbody></tbody>
        </table>
    </div>
</div>
```

`dashboard.js` — replace carousel logic with simple table rendering:
```javascript
function renderMiniTable(tableId, items, columns) {
    var tbody = document.querySelector('#' + tableId + ' tbody');
    if (!items || items.length === 0) {
        tbody.innerHTML = '<tr><td colspan="' + columns.length + '" class="text-center text-secondary">No data</td></tr>';
        return;
    }
    tbody.innerHTML = items.map(function(item) {
        return '<tr>' + columns.map(function(col) {
            return '<td>' + col.format(item) + '</td>';
        }).join('') + '</tr>';
    }).join('');
}
```

Each category gets 3 key columns:
- **High Alch:** Name, Profit, ROI%
- **Flipping:** Name, GP/hr, ROI%
- **Smithing:** Name, Profit/Bar, ROI%
- **Herblore:** Name, Profit/Unit, Method

**Tests:** Manual — verify all 5 items visible per category without interaction.

**Dependencies:** None.

---

## Improvement 11 — Trend Arrows from Window Data

**Problem:** Users see current prices but no indication of direction. Is the margin
widening or narrowing? The time window data implicitly contains trend information.

**Change:** Compare 5m average to 1h average to derive a simple trend direction for
both buy and sell prices. Display as arrows in the flipping table.

**Files:**
- `Core/Entities/FlipCandidate.cs` — add `BuyTrend`, `SellTrend` fields
- `Core/Services/FlipCalculator.cs` — calculate trends from window data
- `Web/wwwroot/js/flipping-filter.js` — render trend arrows

**Changes:**

`FlipCandidate.cs`:
```csharp
/// <summary>Buy price trend: 1 = rising, 0 = stable, -1 = falling.</summary>
public int BuyTrend { get; set; }

/// <summary>Sell price trend: 1 = rising, 0 = stable, -1 = falling.</summary>
public int SellTrend { get; set; }
```

`FlipCalculator.cs` — add helper:
```csharp
private static int ComputeTrend(int? shortTermPrice, int? longTermPrice)
{
    if (!shortTermPrice.HasValue || !longTermPrice.HasValue || longTermPrice.Value == 0)
        return 0;
    var pctChange = (double)(shortTermPrice.Value - longTermPrice.Value) / longTermPrice.Value * 100;
    if (pctChange > 2.0) return 1;   // Rising
    if (pctChange < -2.0) return -1; // Falling
    return 0;                         // Stable
}
```

In `CalculateFlip`, compute and assign:
```csharp
var has5m = priceData.TimeWindows.TryGetValue(TimeWindow.FiveMinute, out var w5m);
var has1h = priceData.TimeWindows.TryGetValue(TimeWindow.OneHour, out var w1h);

BuyTrend = has5m && has1h ? ComputeTrend(w5m.AvgBuyPrice, w1h.AvgBuyPrice) : 0,
SellTrend = has5m && has1h ? ComputeTrend(w5m.AvgSellPrice, w1h.AvgSellPrice) : 0,
```

`flipping-filter.js` — in `renderTable()`, add trend arrows next to Buy/Sell columns:
```javascript
function trendArrow(trend) {
    if (trend === 1) return ' <span class="profit-positive" title="Rising">▲</span>';
    if (trend === -1) return ' <span class="profit-negative" title="Falling">▼</span>';
    return '';
}

// In the Buy column cell:
formatGp(item.recommendedBuyPrice) + trendArrow(item.buyTrend)
// In the Sell column cell:
formatGp(item.recommendedSellPrice) + trendArrow(item.sellTrend)
```

**Tests:**
- `FlipCalculatorTests`: `ComputeTrend_Rising_Returns1` — 5m=110, 1h=100 → 1
- `FlipCalculatorTests`: `ComputeTrend_Falling_ReturnsMinus1` — 5m=90, 1h=100 → -1
- `FlipCalculatorTests`: `ComputeTrend_Stable_Returns0` — 5m=101, 1h=100 → 0
- `FlipCalculatorTests`: `ComputeTrend_NullPrices_Returns0`

**Dependencies:** None.

---

## Improvement 12 — Item Icons

**Problem:** The `/mapping` endpoint returns icon filenames for every item. They are
stored in `ItemMapping.Icon` but never rendered. Item icons make tables scannable.

**Change:** Display item icons next to item names in all tables.

**Files:**
- `Core/Entities/FlipCandidate.cs` — add `IconUrl` field
- `Core/Entities/HighAlchItem.cs` — add `IconUrl` field
- `Core/Entities/SmithingItem.cs` — add `IconUrl` field
- `Core/Entities/HerbloreItem.cs` — add `IconUrl` field
- `Core/Services/FlipAnalyzer.cs` — pass icon through
- `Core/Services/HighAlchingService.cs` — pass icon through
- `Core/Services/SmithingService.cs` — pass icon through
- `Core/Services/HerbloreService.cs` — pass icon through
- `Web/wwwroot/js/flipping-filter.js` — render icon in name column
- `Web/wwwroot/js/highalch-filter.js` — same
- `Web/wwwroot/js/smithing-filter.js` — same
- `Web/wwwroot/js/herblore-filter.js` — same
- `Web/wwwroot/css/site.css` — icon sizing

**Changes:**

The OSRS Wiki hosts item icons at:
```
https://oldschool.runescape.wiki/images/{icon_filename}
```

Where `icon_filename` is the value from the `/mapping` endpoint's `icon` field
(e.g., `"Dragon bones.png"` → URL-encoded as `Dragon%20bones.png`).

Add `IconUrl` to each entity:
```csharp
/// <summary>URL to the item's icon image.</summary>
public string? IconUrl { get; set; }
```

In each service, when building the entity from mapping data:
```csharp
IconUrl = !string.IsNullOrEmpty(mapping.Icon)
    ? "https://oldschool.runescape.wiki/images/" + Uri.EscapeDataString(mapping.Icon)
    : null
```

In each JS `renderTable()`, prepend icon to name column:
```javascript
var iconHtml = item.iconUrl
    ? '<img src="' + escapeHtml(item.iconUrl) + '" class="item-icon" alt="" loading="lazy"> '
    : '';
var nameHtml = iconHtml + escapeHtml(item.name);
```

CSS:
```css
.item-icon {
    width: 24px;
    height: 24px;
    vertical-align: middle;
    margin-right: 4px;
}

@media (max-width: 768px) {
    .item-icon { width: 18px; height: 18px; }
}
```

**Tests:**
- Verify `IconUrl` is populated when `mapping.Icon` is set
- Verify `IconUrl` is null when `mapping.Icon` is null/empty
- Verify URL encoding of special characters (spaces → `%20`)

**Dependencies:** None. Requires external network access to OSRS Wiki for images
(consider lazy loading and fallback for missing icons).

---

## Implementation Sequence

Recommended order by effort and impact:

```
Phase 1 — Quick wins (small effort, high impact)
  Improvement 1  — Persist filter settings        [JS only, ~1 hour]
  Improvement 11 — Trend arrows                   [Core + JS, ~2 hours]
  Improvement 9  — CSV export                     [JS only, ~1 hour]

Phase 2 — Accuracy signals (small-medium effort, high accuracy value)
  Improvement 2  — Data age per item              [Core + JS, ~2 hours]
  Improvement 3  — Volume imbalance display       [JS only, ~1 hour]
  Improvement 4  — Instant vs recommended gap     [Core + JS, ~2 hours]

Phase 3 — Usability features (medium effort, high usability value)
  Improvement 7  — Item search + favorites        [JS + CSS, ~3 hours]
  Improvement 12 — Item icons                     [Core + JS + CSS, ~3 hours]
  Improvement 5  — GP/hr for High Alching         [Core + JS, ~2 hours]

Phase 4 — Infrastructure (medium effort, ongoing value)
  Improvement 8  — Auto-refresh                   [Controllers + JS, ~4 hours]
  Improvement 6  — Output volume Smithing/Herblore [Core + JS, ~3 hours]
  Improvement 10 — Dashboard mini-tables          [View + JS, ~2 hours]
```

---

## Token Estimates

| # | Improvement | Complexity | Low | High |
|---|-----------|-----------|-----|------|
| 1 | Persist filter settings | Small (JS only) | 15,000 | 25,000 |
| 11 | Trend arrows | Small (Core + JS) | 20,000 | 35,000 |
| 9 | CSV export | Small (JS only) | 15,000 | 25,000 |
| 2 | Data age per item | Small (Core + JS) | 20,000 | 35,000 |
| 3 | Volume imbalance | Small (JS only) | 10,000 | 20,000 |
| 4 | Instant vs recommended gap | Small (Core + JS) | 20,000 | 35,000 |
| 7 | Item search + favorites | Medium (JS + CSS) | 25,000 | 45,000 |
| 12 | Item icons | Medium (Core + JS + CSS) | 25,000 | 45,000 |
| 5 | GP/hr for High Alching | Small (Core + JS) | 20,000 | 35,000 |
| 8 | Auto-refresh | Medium (Controllers + JS) | 30,000 | 50,000 |
| 6 | Output volume Smithing/Herblore | Medium (Core + JS) | 25,000 | 40,000 |
| 10 | Dashboard mini-tables | Small (View + JS) | 15,000 | 25,000 |
| **Total** | | | **240,000** | **415,000** |
