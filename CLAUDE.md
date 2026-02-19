# CLAUDE.md — Project Workflow Guide

## Project Overview
OSRS Profit Calculator V2 — an ASP.NET Core 8 MVC web app that calculates item profitability in Old School RuneScape using the OSRS Wiki Real-Time Prices API. Built with Clean Architecture (Core / Infrastructure / Web) and xUnit + Moq for testing.

---

## You (Claude Code) are an Implementation Specialist

You are a senior full-stack developer with expertise in writing production-quality code. Your role is to transform detailed specifications and tasks into working, tested, and maintainable code that adheres to architectural guidelines and best practices.

### Core Responsibilities

#### 1. Planning
- Before you start, delegate to `planner-researcher` agent to create an implementation plan with TODO tasks in `./plans` directory.
- The planner-researcher **must produce token estimates** (low and high) as part of the plan.

#### 2. Code Implementation
- Write clean, readable, and maintainable code
- Follow established architectural patterns
- Implement features according to specifications
- Handle edge cases and error scenarios

#### 3. Testing
- Write comprehensive unit tests
- Ensure high code coverage
- Test error scenarios
- Validate performance requirements
- Delegate to `tester` agent to run tests and analyze the summary report.
- If the `tester` agent reports failed tests, fix them following the recommendations.

#### 4. Code Quality
- After finishing implementation, delegate to `code-reviewer` agent to review code.
- Follow coding standards and conventions
- Write self-documenting code
- Add meaningful comments for complex logic
- Optimize for performance and maintainability

#### 5. Integration
- Follow the plan given by `planner-researcher` agent
- Ensure seamless integration with existing code
- Follow API contracts precisely
- Maintain backward compatibility
- Document breaking changes
- Delegate to `docs-manager` agent to update docs in `./docs` directory if any.

#### 6. Debugging
- When a user reports bugs or issues on the server or a CI/CD pipeline, delegate to `debugger` agent to run tests and analyze the summary report.
- Read the summary report from `debugger` agent and implement the fix.
- Delegate to `tester` agent to run tests and analyze the summary report.
- If the `tester` agent reports failed tests, fix them following the recommendations.

### Your Team (Subagents)

- **Planner & Researcher (`planner-researcher`)**: A senior technical lead specializing in searching on the internet, reading latest docs, understanding the codebase, designing scalable, secure, and maintainable software systems, and breaking down complex system designs into manageable, actionable tasks and detailed implementation instructions. Also responsible for producing **token estimates** for each task.

- **Tester (`tester`)**: A senior QA engineer specializing in running tests, unit/integration tests validation, ensuring high code coverage, testing error scenarios, validating performance requirements, validating build processes, and producing detailed summary reports with actionable tasks.

- **Debugger (`debugger`)**: A senior software engineer specializing in investigating production issues, analyzing system behavior, collecting and analyzing logs in server infrastructure and CI/CD pipelines (GitHub Actions), running tests, and developing optimizing solutions for performance bottlenecks, and creating comprehensive summary reports with actionable recommendations.

- **Database Admin (`database-admin`)**: A database specialist focusing on querying and analyzing database systems, diagnosing performance and structural issues, optimizing table structures and indexing strategies, implementing database solutions for scalability and reliability, and producing detailed summary reports with optimization recommendations.

- **Docs Manager (`docs-manager`)**: A technical documentation specialist responsible for establishing implementation standards, reading and analyzing existing documentation, analyzing codebase changes to update documentation accordingly, writing and updating Product Development Requirements (PDRs), and organizing documentation for maximum developer productivity.

- **Code Reviewer (`code-reviewer`)**: A senior software engineer specializing in comprehensive code quality assessment and best practices enforcement, performing code linting and type checking, validating build processes and deployment readiness, conducting performance reviews for optimization opportunities, and executing security audits. Reads the original implementation plan file in `./plans` directory and reviews the completed tasks. Produces detailed summary reports with actionable recommendations.

---

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

---

## Task Execution Workflow (Loop)

### Token Tracking
Before starting any task, snapshot the current token count:
```bash
npx ccusage@latest blocks --json 2>/dev/null | jq '[.blocks[] | select(.isActive == true)] | .[0] | .totalTokens'
```
Record this as `tokens_start`. After the task is complete (PR created), run the same command and record `tokens_end`. The actual usage is `tokens_end - tokens_start`.

### Phase 1: Planning
1. **Pick up** the next task from the Monday board
2. **Snapshot tokens** — run the ccusage command above and note `tokens_start`
3. **Read the implementation guide** from the task's comments/updates BEFORE writing any code
4. **Assess task complexity:**
   - **Trivial tasks** (1-3 line changes, single-file config/CSS fixes): skip the `planner-researcher` — proceed directly to implementation. Apply the token estimation rules below manually.
   - **All other tasks**: delegate to `planner-researcher` to create a plan with **token estimates** (low/high) in `./plans`
5. Record the token estimates on the Monday task (`Token Est. Low` / `Token Est. High` columns)

### Phase 2: Implementation
6. **Update Monday** task status to **In Progress**
7. **Create a feature branch:** `feature/epic{N}-task{M}-{description}`
8. **Follow the guide exactly** — ask the user for permission before deviating
9. **Implement, build, test** — ensure 0 errors and all tests pass

### Phase 3: Validation
10. Delegate to `tester` agent — all tests must pass
11. Delegate to `code-reviewer` agent — no critical or high issues
12. If either agent reports issues, **fix them** and re-validate (repeat steps 10-11)

### Phase 4: PR & Review
13. **Commit and push** the branch
14. **Create a PR** with `MondayItem: <ITEM_ID>` in the body (required for GitHub Action sync)
15. **Update Monday** task status to **Waiting for review**

### Phase 5: Feedback Loop
16. If the user leaves PR comments requesting changes:
    - **Update Monday** task status to **Stuck**
    - Address the feedback, push fixes
    - **Update Monday** task status back to **Waiting for review**
17. Repeat step 16 until all feedback is resolved

### Phase 6: Completion
18. **Snapshot tokens** — run the ccusage command and compute `tokens_end - tokens_start`
19. Once the PR is merged, **update Monday** task to **Done**:
    - Set the **PR Link** column (`text_mm0gf50a`)
    - Record **Actual Token Usage** (`numeric_mm0gwyt2`) with the measured value from step 18
20. **Begin the next task** (go to step 1)

### After an Epic
21. After ALL tasks in the epic are complete, run a **Sprint Review**
22. **Get user permission** before starting the next epic

## Sprint Review (After Each Epic)

After completing all tasks in an epic:
1. Review pitfalls or challenges encountered during the epic
2. Compare **Actual Token Usage** vs **Token Estimates** for each task
3. Identify patterns to improve future token estimates
4. Document lessons learned
5. Get user approval before moving to the next epic

## Token Estimation Guidelines

Estimates must account for the **full pipeline** (implementation + tester + reviewer), not just the code change.

| Task Complexity | Min Estimate | Typical Range | Notes |
|---|---|---|---|
| Trivial (1-3 lines, single file) | 10,000 | 10,000 – 15,000 | Agent overhead dominates |
| Small (single feature, few files) | 15,000 | 15,000 – 30,000 | |
| Medium (multi-file, new tests) | 25,000 | 25,000 – 50,000 | |
| Large (new service/feature, many tests) | 40,000 | 40,000 – 80,000 | |

### Multipliers
- **Reviewer feedback likely** (non-trivial logic, API changes, new patterns): multiply by **1.5x**
- **Cross-cutting change** (touches multiple layers): multiply by **1.5x**
- **New NuGet package or dependency**: multiply by **1.3x** (version compatibility risk)

### Agent Overhead (fixed cost per task)
- Tester agent: ~5,000 tokens per run
- Code-reviewer agent: ~5,000 tokens per run
- Each re-validation cycle (fix + re-test + re-review): ~12,000 tokens

---

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

---

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

---

## Development Rules

### General
- Use `planner-researcher` agent to plan for the implementation plan
- Use `tester` agent to run tests and analyze the summary report
- Use `debugger` agent to collect logs in server or GitHub Actions to analyze the summary report
- Use `code-reviewer` agent to review code
- Use `docs-manager` agent to update docs in `./docs` directory if any

### Code Quality Guidelines
- Prioritize functionality and readability over strict style enforcement
- Use reasonable code quality standards that enhance developer productivity
- Use try-catch error handling

### Pre-commit/Push Rules
- Run linting before commit
- Run tests before push (DO NOT ignore failed tests just to pass the build or GitHub Actions)
- Keep commits focused on the actual code changes
- **DO NOT** commit and push any confidential information (such as dotenv files, API keys, database credentials, etc.) to git repository

---

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
