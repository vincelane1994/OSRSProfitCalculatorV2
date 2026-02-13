# CLAUDE.md — Project Workflow Guide

## Project Overview
OSRS Profit Calculator V2 — an ASP.NET Core 8 MVC web app that calculates item profitability in Old School RuneScape using the OSRS Wiki Real-Time Prices API. Built with Clean Architecture (Core / Infrastructure / Web) and xUnit + Moq for testing.

## Monday Board Integration

### Board IDs
- **Tasks board:** 18399588298
- **Epics board:** 18399588299

### Key Column IDs (Tasks board)
| Column | ID | Type |
|---|---|---|
| Status | `task_status` | status |
| PR Link | `text_mm0gf50a` | text |
| Actual Token Usage | `numeric_mm0gwyt2` | numbers |
| Token Est. Low | `numeric_mm0gj4g3` | numbers |
| Token Est. High | `numeric_mm0gxc8v` | numbers |
| Epic | `task_epic` | board_relation |
| Priority | `task_priority` | status |
| Type | `task_type` | status |

### Status Labels (task_status)
| Label | Index | Meaning |
|---|---|---|
| Ready to start | 11 | Not yet started |
| In Progress | 0 | Currently being worked on |
| Waiting for review | 3 | PR created, awaiting review |
| Pending Deploy | 2 | Approved, awaiting deploy |
| Done | 1 | Merged and complete |
| Stuck | 103 | Blocked or changes requested |

## Task Execution Workflow (Loop)

1. **Pick up** the next task from the Monday board
2. **Read the implementation guide** from the task's comments/updates BEFORE writing any code
3. **Follow the guide exactly** — ask the user for permission before deviating
4. **Create a feature branch:** `feature/epic{N}-task{M}-{description}`
5. **Implement, build, test** — ensure 0 errors and all tests pass
6. **Commit and push** the branch
7. **Create a PR** with `MondayItem: <ITEM_ID>` in the body (required for GitHub Action sync)
8. **Update Monday** task status to **Waiting for review**
9. **Execute on any PR review comments** — address feedback, push fixes
10. If no more PR comments to address, **begin the next task** (go to step 1)
11. Once a PR is merged, **update Monday** task to **Done** — set PR Link and record **Actual Token Usage**
12. After ALL tasks in the epic are complete, run a **Sprint Review**
13. **Get user permission** before starting the next epic

## Sprint Review (After Each Epic)

After completing all tasks in an epic:
1. Review pitfalls or challenges encountered during the epic
2. Compare **Actual Token Usage** vs **Token Estimates** for each task
3. Identify patterns to improve future token estimates
4. Document lessons learned
5. Get user approval before moving to the next epic

## Creating New Tasks

When a gap, bug, or missing piece is found:
1. Create a new item on the Tasks board (18399588298) with a descriptive name
2. Add a detailed description as an update/comment with an implementation guide
3. Include: steps to follow, files to modify, expected behavior
4. Link to the appropriate Epic via the `task_epic` column
5. Set priority and type columns

## Creating New Epics

When a new feature area is identified:
1. Create a new item on the Epics board (18399588299)
2. Add a description of the feature scope
3. Break it down into individual tasks on the Tasks board
4. Each task gets an implementation guide in its comments
5. Link all tasks to the epic via `task_epic`

## Git & PR Conventions

### Branching
- Pattern: `feature/epic{N}-task{M}-{description}`
- One branch per task, each with its own PR
- Branches may be chained (task2 based on task1) within an epic

### Commit Messages
- Short summary line describing what changed
- Longer body explaining why when non-obvious
- End with: `Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>`

### PR Body Format
Must include `MondayItem: <NUMERIC_ID>` for the Monday sync GitHub Action:
```
## Summary
- Bullet points describing changes

## Test plan
- [x] Build succeeds
- [x] All tests pass
- [ ] Manual verification steps

MondayItem: 11263851512
```

### GitHub Actions
- **claude.yml** — Claude Code PR assistant, triggered by `@claude` mentions
- **monday-sync.yml** — Syncs PR lifecycle to Monday task statuses automatically

## Architecture Rules

### OSRSTools.Core (Domain Layer)
- `Entities/` — Domain models (e.g., ItemMapping, HighAlchItem)
- `ValueObjects/` — Immutable value types (e.g., ProfitCalculation as record struct)
- `Interfaces/` — Repository and service contracts
- `Services/` — Domain services that depend ONLY on Core interfaces
- `Configuration/` — Settings POCOs
- **No dependencies** on Infrastructure or Web

### OSRSTools.Infrastructure (Data Layer)
- `Api/` — HTTP clients (OsrsWikiApiClient implements IItemMappingRepository + IPriceRepository)
- `Api/Dtos/` — API deserialization DTOs
- `Caching/` — Cache implementations (MemoryCacheService)
- `Services/` — Data orchestrators (DataFetchService)
- Depends on Core only

### OSRSTools.Web (Presentation Layer)
- `Controllers/` — MVC controllers with try/catch error handling
- `ViewModels/` — One ViewModel per view
- `Views/` — Razor templates
- `wwwroot/` — Static assets (CSS, JS)
- `Program.cs` — All DI wiring
- Depends on Core and Infrastructure

### OSRSTools.UnitTests
- Mirrors source folder structure: `Core/`, `Infrastructure/`, `Web/`
- Framework: xUnit + Moq
- Test class naming: `{ClassName}Tests`
- Test method naming: `{Method}_{Scenario}_{Expected}`
- Uses AAA pattern (Arrange-Act-Assert)

## DI Registration Patterns

### Standard registrations
```csharp
builder.Services.AddScoped<IService, ServiceImpl>();
```

### OsrsWikiApiClient (shared instance pattern)
`AddHttpClient<T>()` registers as **transient**. Since OsrsWikiApiClient implements both `IItemMappingRepository` and `IPriceRepository` and shares internal state (`_highAlchValues` dictionary), all injection points must resolve to the **same instance per scope**:
```csharp
builder.Services.AddHttpClient<OsrsWikiApiClient>();
builder.Services.AddScoped<OsrsWikiApiClient>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient(nameof(OsrsWikiApiClient));
    return ActivatorUtilities.CreateInstance<OsrsWikiApiClient>(sp, httpClient);
});
builder.Services.AddScoped<IItemMappingRepository>(sp => sp.GetRequiredService<OsrsWikiApiClient>());
builder.Services.AddScoped<IPriceRepository>(sp => sp.GetRequiredService<OsrsWikiApiClient>());
```

### Lifetime rules
- **Scoped:** Domain services, infrastructure services, API clients
- **Singleton:** Cache service (MemoryCacheService)
- **Never** register a singleton that depends on a scoped/transient service

## Configuration

Settings are bound via `IOptions<T>` pattern:
```csharp
builder.Services.Configure<OsrsApiSettings>(builder.Configuration.GetSection("OsrsApi"));
```
Config classes live in `Core/Configuration/`. Settings are in `appsettings.json`.

## Known Pitfalls

1. **HttpClient URL resolution:** `BaseAddress` MUST end with `/`. Endpoints MUST be relative (no leading `/`). Otherwise `HttpClient` drops the path prefix and you get 404s.

2. **Typed HttpClient lifetime:** `AddHttpClient<T>()` registers as transient. If the class holds per-request state (like `_highAlchValues`), you must override with a scoped factory (see DI section above).

3. **File locks during build:** If `OSRSTools.Web.exe` is running, builds will fail with MSB3027. Kill the process first: `taskkill //F //IM "OSRSTools.Web.exe"`.

4. **Test Volume24Hr:** When creating test price data, include `TimeWindow.TwentyFourHour` in the time windows dictionary — `Volume24Hr` reads from that window specifically.

5. **Nature Rune in tests:** Keep Nature Rune (ID 561) in the prices dictionary but NOT in the mappings dictionary, or it will appear as a regular item in results.

6. **gh CLI path on Windows:** `gh` may not be on PATH. Full path: `"/c/Program Files/GitHub CLI/gh.exe"`.

7. **JSON serialization casing:** `System.Text.Json.JsonSerializer.Serialize()` defaults to PascalCase. When serializing C# objects for JavaScript, always use `new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }` since JS expects camelCase property names.

## General Rules

- **No Python.** This is a .NET/C#/JavaScript codebase only. Do not use Python for any scripts, tools, or utilities.
