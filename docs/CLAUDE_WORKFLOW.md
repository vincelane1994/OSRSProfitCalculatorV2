# Claude Agent Workflow Guide

This document describes the standard workflow for any Claude agent working on the OSRS Profit Calculator V2 project. Follow these steps for every task.

---

## Project Overview

OSRS Profit Calculator V2 is an ASP.NET Core 8.0 MVC app built with Clean Architecture. It calculates profit opportunities in Old School RuneScape (OSRS) — a game where players trade items on the Grand Exchange (GE).

**Solution structure:**
```
OSRSProfitCalculatorV2/
├── src/
│   ├── OSRSTools.Core/          — Domain layer (zero external deps)
│   │   ├── Configuration/       — Settings POCOs (bound from appsettings)
│   │   ├── Entities/            — Domain models (ItemMapping, ItemPriceData, etc.)
│   │   ├── Interfaces/          — All abstractions (repos, services)
│   │   ├── ValueObjects/        — Immutable calculation results (readonly record struct)
│   │   └── Services/            — Pure domain logic (no I/O)
│   ├── OSRSTools.Infrastructure/ — External concerns
│   │   ├── Api/                 — OSRS Wiki API client + DTOs
│   │   ├── Caching/             — MemoryCache implementation
│   │   ├── Services/            — DataFetchService orchestrator
│   │   └── (future: Export/, GoogleSheets/, Persistence/)
│   └── OSRSTools.Web/           — ASP.NET Core MVC entry point
│       ├── Controllers/
│       ├── Views/
│       ├── ViewModels/
│       └── wwwroot/ (css, js)
├── tests/
│   └── OSRSTools.UnitTests/     — xUnit + Moq
└── docs/                        — Project documentation
```

---

## Scope Discipline (IMPORTANT)

Each Monday task description lists the **exact files** you should create or modify. Follow these rules strictly to keep token usage low:

1. **Only touch files listed in the task description.** Do not explore unrelated code.
2. **Do not read files you don't need.** The task description tells you which existing files to reference as patterns — read only those.
3. **Do not refactor existing code** unless the task explicitly says to.
4. **Do not add features beyond what the task describes.** If you think something is missing, note it and move on.
5. **Do not run the full app** unless the task is a controller/view task that requires visual verification. `dotnet build` and `dotnet test` are sufficient for most tasks.
6. **Keep PRs focused.** One task = one branch = one PR. Never bundle multiple tasks.

---

## Task Workflow (Step by Step)

### 1. Pick Up a Task
- Tasks are on the Monday.com Tasks board (ID: `18399588298`)
- Each task has an epic, priority, status, and Monday Item ID
- Only work on tasks with status "Ready to start" (index 11)
- **Read the task description carefully** — it lists exactly which files to create, which files to reference as patterns, and what the acceptance criteria are

### 2. Create a Feature Branch
```bash
git checkout main
git pull origin main
git checkout -b feature/epic{N}-task{M}-short-description
```
**Branch naming:** `feature/epic3-task1-highalch-entity`

### 3. Implement the Task
Follow the task description and these conventions:

**Entities** go in `src/OSRSTools.Core/Entities/`
- Use `{ get; init; }` properties
- Add XML doc comments
- Follow the `ItemMapping` pattern

**Interfaces** go in `src/OSRSTools.Core/Interfaces/`
- Name: `I{ServiceName}.cs`
- Add XML doc comments on every method
- No implementation details — pure contracts

**Value Objects** go in `src/OSRSTools.Core/ValueObjects/`
- Use `public readonly record struct`
- Include static factory method if complex construction needed
- Follow the `TaxCalculation` pattern

**Services** (domain logic, no I/O) go in `src/OSRSTools.Core/Services/`
- Inject dependencies via constructor (interfaces + IOptions<T>)
- No direct HTTP calls, file I/O, or database access
- Follow the `ProfitCalculationService` pattern

**Infrastructure services** go in `src/OSRSTools.Infrastructure/`
- Implement Core interfaces
- OK to depend on external packages (HttpClient, ClosedXML, etc.)
- Follow the `OsrsWikiApiClient` and `DataFetchService` patterns

**Controllers** go in `src/OSRSTools.Web/Controllers/`
- Inject services via constructor
- Keep controllers thin — delegate to services
- Return `View(viewModel)` for page actions

**Views** go in `src/OSRSTools.Web/Views/{ControllerName}/`
- Use Razor syntax with `@model` directive
- Serialize model to JSON for client-side filtering: `@Html.Raw(System.Text.Json.JsonSerializer.Serialize(Model))`
- Follow dark theme from `site.css` (CSS variables: `--bg-primary`, `--text-primary`, etc.)

**ViewModels** go in `src/OSRSTools.Web/ViewModels/`
- View-specific models only — not domain entities

**Unit Tests** go in `tests/OSRSTools.UnitTests/`
- Mirror the src/ folder structure
- Use xUnit `[Fact]` and `[Theory]` attributes
- Use Moq for mocking interfaces
- Use `FakeHttpHandler` for HTTP tests (see `OsrsWikiApiClientTests.cs`)
- Name: `{ClassName}Tests.cs`

### 4. Register in DI (if applicable)
When adding new services, register them in `src/OSRSTools.Web/Program.cs`:
```csharp
// Core services
builder.Services.AddScoped<IHighAlchingService, HighAlchingService>();

// Infrastructure (typed HttpClient example)
builder.Services.AddHttpClient<OsrsWikiApiClient>();
builder.Services.AddScoped<IItemMappingRepository>(sp => sp.GetRequiredService<OsrsWikiApiClient>());
```

### 5. Add Nav Links (if adding a calculator page)
Add sidebar link in `src/OSRSTools.Web/Views/Shared/_Layout.cshtml` under the "Profit Calculators" section.

### 6. Build and Test
```bash
dotnet build OSRSProfitCalculatorV2.sln
dotnet test tests/OSRSTools.UnitTests --verbosity normal
```
All tests must pass before committing.

### 7. Commit
```bash
git add <specific-files>
git commit -m "$(cat <<'EOF'
Short summary of what changed

Longer description of why and any design decisions.

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
EOF
)"
```

### 8. Push and Create PR
```bash
git push -u origin feature/epic{N}-task{M}-short-description

gh pr create --title "Epic {N} Task {M}: Short title" --body "$(cat <<'EOF'
## Summary
- Bullet points of what was done

## Test plan
- [x] Tests pass
- [ ] Manual testing notes

MondayItem: {MONDAY_ITEM_ID}

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

**CRITICAL:** Include `MondayItem: {MONDAY_ITEM_ID}` in the PR body. This triggers the Monday-GitHub sync workflow to update the task status automatically.

---

## Key Conventions

### OSRS Domain Knowledge
- **Grand Exchange (GE):** The in-game marketplace where players buy/sell items
- **GE Tax:** 2% tax on sales, capped at 5M GP (configurable in `TaxSettings`)
- **Buy Limit:** Each item has a max purchase quantity per 4-hour cycle
- **High Alchemy:** A spell that converts items to gold (fixed price per item)
- **Flipping:** Buying items at low price, selling at high price for margin
- **Nature Rune:** Required consumable for High Alchemy spell (item ID: 561)

### API Endpoints
All prices come from `https://prices.runescape.wiki/api/v1/osrs`:
- `/mapping` — Item IDs, names, buy limits, members status
- `/latest` — Most recent instant buy/sell prices
- `/5m`, `/1h`, `/6h`, `/24h` — Time-window averaged prices + volumes

### Price Recommendation Weights
Weighted average across time windows (configurable in `PriceWeightSettings`):
- 5-minute: 10%, 1-hour: 35%, 6-hour: 35%, 24-hour: 20%
- Missing windows have weight redistributed proportionally

### Existing Patterns to Follow
- **Multi-output profit:** `CalculateMultiOutputProfit(inputPrice, outputPrice, outputPerInput, quantity)` — used for cannonballs (4 per bar) and dart tips (10 per bar)
- **Simple profit:** `CalculateSimpleProfit(buyPrice, sellPrice, quantity)` — used for high alching
- **Client-side filtering:** Serialize model as JSON in view, filter with JavaScript `applyFilters()` function
- **Caching:** Use `ICacheService` — mappings cached 24h, prices cached 5m

### Monday.com Integration
- Tasks Board: `18399588298`
- Epics Board: `18399588299`
- Status values: 0=In Progress, 1=Done, 11=Ready to start, 103=Stuck
- Always include `MondayItem: <ID>` in PR body for automatic status sync

---

## Common Item IDs (Reference)

### Bars
| Item | ID |
|---|---|
| Bronze bar | 2349 |
| Iron bar | 2351 |
| Steel bar | 2353 |
| Mithril bar | 2359 |
| Adamant bar | 2361 |
| Rune bar | 2363 |

### Runes
| Item | ID |
|---|---|
| Nature rune | 561 |

### Key Items
| Item | ID |
|---|---|
| Cannonball | 2 |
| Vial of water | 227 |

---

## Pitfalls to Avoid
See `docs/PITFALLS.md` for a running list of issues we've encountered and their solutions.
