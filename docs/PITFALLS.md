# Pitfalls & Lessons Learned

A running log of issues encountered during development so we don't repeat them.

---

## GitHub Actions

### 1. Never inline `${{ }}` expressions in shell `run:` blocks
**Date:** 2026-02-12
**PR:** #10 (Monday-GitHub sync workflow)

**Problem:** Using `${{ github.event.pull_request.body }}` directly inside a `run:` script causes the raw PR body to be interpolated as shell code. Backticks, angle brackets, `$()`, and other special characters in the PR body get executed as commands.

**Error:**
```
monday-sync.yml: No such file or directory
MONDAY_API_KEY: command not found
command substitution: syntax error near unexpected token `newline'
```

**Fix:** Always pass GitHub context values through `env:` blocks. The runner safely quotes environment variables.

```yaml
# BAD - shell injection risk
run: |
  BODY="${{ github.event.pull_request.body }}"

# GOOD - safe via env var
env:
  PR_BODY: ${{ github.event.pull_request.body }}
run: |
  echo "$PR_BODY"
```

Also use `jq` for building JSON payloads from untrusted content instead of manual string escaping.

---

## Monday.com API

### 2. Status labels cannot be created via API
**Date:** 2026-02-12

**Problem:** Monday's GraphQL API does not support creating new status labels programmatically. The `change_column_metadata` mutation only accepts `title` and `description` properties — not `labels`. Attempting to set a status with a label that doesn't exist returns an error.

**Workaround:** Use an existing status label that's semantically close, or create the label manually in Monday's board UI first. We mapped "Changes Requested" → "Stuck" (index 103).

---

## Git / GitHub

### 3. `.gitignore` `*.json` rule blocks `launchSettings.json`
**Date:** 2026-02-11

**Problem:** A broad `*.json` rule in `.gitignore` excluded `Properties/launchSettings.json`, which is needed for the project to build/run after cloning. The build failed on a fresh clone because the launch profile was missing.

**Fix:** Add explicit exceptions:
```gitignore
*.json
!appsettings.json
!appsettings.Development.json
!Properties/launchSettings.json
!**/Properties/launchSettings.json
```

---

## C# / .NET

### 4. xUnit boundary test precision matters
**Date:** 2026-02-11
**PR:** #8 (Value object tests)

**Problem:** A test named `Calculate_JustAboveCap_IsCapped` used `250_000_001 * 0.02 = 5_000_000.02`, but `Math.Floor` truncates to `5_000_000` which equals the cap — not above it. The test asserted `WasCapped = true` but got `false`.

**Fix:** Use a value that clearly exceeds the cap after calculation. Changed to `300_000_000` (300M × 0.02 = 6M, clearly above the 5M cap).

**Lesson:** When testing boundary conditions, ensure the math actually crosses the boundary after all rounding/truncation is applied.

---

## GitHub CLI

### 5. `gh` CLI may not be in PATH after install
**Date:** 2026-02-11

**Problem:** After installing GitHub CLI via `winget`, the `gh` command wasn't found in the current shell session because the PATH hadn't been refreshed.

**Workaround:** Use the full path: `"/c/Program Files/GitHub CLI/gh.exe"` or restart the terminal session.

---

## Git Repository Setup

### 6. Push rejected when remote has existing content
**Date:** 2026-02-11

**Problem:** `git push` failed when the remote repo already had a `README.md` from GitHub's repo creation. Git rejected the push because the histories were unrelated.

**Fix:**
```bash
git pull origin main --allow-unrelated-histories
git push origin main
```
