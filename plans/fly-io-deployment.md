# Fly.io Deployment Plan for OSRSProfitCalculatorV2

**Date:** 2026-03-24
**Status:** Draft

---

## Overview

Deploy the OSRSTools.Web ASP.NET Core 8 MVC application to Fly.io using a multi-stage Docker build. The app is a stateless web application that calls the OSRS Wiki Real-Time Prices API -- no database is required. This makes it an ideal candidate for Fly.io's container-based hosting.

## Project Structure Summary

```
OSRSProfitCalculatorV2/
  OSRSProfitCalculatorV2.sln
  src/
    OSRSTools.Core/          (Domain layer, net8.0)
    OSRSTools.Infrastructure/ (Data layer, net8.0)
    OSRSTools.Web/           (Presentation layer, net8.0, depends on Core + Infrastructure)
  tests/
    OSRSTools.UnitTests/
```

**Key observations:**
- Target framework: `net8.0`
- The solution file is at the repo root; project references use relative paths (`../OSRSTools.Core/`)
- No existing Dockerfile or fly.toml
- No database -- all data comes from OSRS Wiki API at runtime
- Configuration via `appsettings.json` using `IOptions<T>` pattern
- Settings sections: OsrsApi, Tax, Cache, PriceWeights, Scoring
- No secrets currently (the API is public, no auth keys needed)
- The app uses `UseHttpsRedirection()` in production (Fly.io handles TLS termination at the proxy)

---

## Architecture Decision: Port Configuration

ASP.NET Core 8 defaults to listening on port `8080` (changed from 5000 in .NET 8). Fly.io's default `internal_port` is also `8080`. This is a natural match -- no custom port configuration is needed.

However, `UseHttpsRedirection()` in Program.cs will cause redirect loops behind the Fly.io proxy (which terminates TLS and forwards plain HTTP internally). We need to handle this by setting `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` so the app trusts Fly.io's forwarded headers.

ASP.NET Core 8 has built-in support for this environment variable, which automatically configures `ForwardedHeadersMiddleware` to trust `X-Forwarded-For` and `X-Forwarded-Proto` headers. This means the app correctly detects HTTPS even though Fly.io proxies HTTP internally. No code changes needed.

---

## Implementation Steps

### Step 1: Create the Dockerfile

**File:** `Dockerfile` (repo root)

```dockerfile
# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (layer caching for restore)
COPY OSRSProfitCalculatorV2.sln ./
COPY src/OSRSTools.Core/OSRSTools.Core.csproj src/OSRSTools.Core/
COPY src/OSRSTools.Infrastructure/OSRSTools.Infrastructure.csproj src/OSRSTools.Infrastructure/
COPY src/OSRSTools.Web/OSRSTools.Web.csproj src/OSRSTools.Web/

# Restore dependencies
RUN dotnet restore src/OSRSTools.Web/OSRSTools.Web.csproj

# Copy everything else
COPY src/ src/

# Publish in Release mode
RUN dotnet publish src/OSRSTools.Web/OSRSTools.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser

COPY --from=build /app/publish .

# Switch to non-root user
USER appuser

# ASP.NET Core 8 defaults to port 8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OSRSTools.Web.dll"]
```

**Key design decisions:**
- Multi-stage build: SDK image (~900MB) for building, aspnet runtime image (~220MB) for running
- Layer caching: copy .csproj files first, then `dotnet restore`, then copy source -- so dependency restore is cached unless project files change
- Non-root user: security best practice
- No `ASPNETCORE_URLS` override needed -- .NET 8 defaults to `http://+:8080`
- Tests are NOT run in the Docker build (they should run in CI/CD before building the image)

### Step 2: Create .dockerignore

**File:** `.dockerignore` (repo root)

```
**/bin/
**/obj/
**/publish/
**/.vs/
**/.vscode/
**/node_modules/
tests/
Files/
plans/
docs/
nul
*.md
.git/
.gitignore
```

**Purpose:** Reduces Docker build context size and prevents unnecessary files from being sent to the Docker daemon. Excluding `tests/` from the image since we don't run tests during Docker build.

### Step 3: Create fly.toml

**File:** `fly.toml` (repo root)

```toml
# See https://fly.io/docs/reference/configuration/ for reference

app = "osrs-profit-calculator"
primary_region = "ewr"  # US East (Newark) -- adjust as needed

[build]
  dockerfile = "Dockerfile"

[env]
  ASPNETCORE_ENVIRONMENT = "Production"
  ASPNETCORE_FORWARDEDHEADERS_ENABLED = "true"

[http_service]
  internal_port = 8080
  force_https = true
  auto_stop_machines = "stop"
  auto_start_machines = true
  min_machines_running = 0
  processes = ["app"]

  [http_service.concurrency]
    type = "requests"
    hard_limit = 250
    soft_limit = 200

[[http_service.checks]]
  grace_period = "10s"
  interval = "30s"
  method = "GET"
  timeout = "5s"
  path = "/"
```

**Configuration rationale:**
- **primary_region = "ewr"**: US East is a good default; change based on your location/audience
- **internal_port = 8080**: matches ASP.NET Core 8 default
- **force_https = true**: Fly.io handles TLS termination; all HTTP requests redirect to HTTPS
- **ASPNETCORE_FORWARDEDHEADERS_ENABLED**: prevents HTTPS redirect loops behind the proxy
- **auto_stop_machines / auto_start_machines**: cost optimization -- machines stop when idle and restart on incoming requests (cold start ~2-5 seconds for .NET)
- **min_machines_running = 0**: allows full stop when idle (set to 1 if you want zero-downtime at all times, but costs more)
- **Health check on `/`**: the HomeController Index action serves as a basic health check

### Step 4: Configuration and Secrets Management

#### Current Configuration Analysis

| Section | Sensitive? | Notes |
|---|---|---|
| OsrsApi.BaseUrl | No | Public API URL |
| OsrsApi.UserAgent | No | Identification string |
| OsrsApi.Endpoints | No | API endpoint paths |
| Tax | No | Game constants |
| Cache | No | Cache durations |
| PriceWeights | No | Algorithm weights |
| Scoring | No | Scoring breakpoints |

**Conclusion:** There are currently NO secrets to manage. The OSRS Wiki API is public and does not require authentication. All configuration values in `appsettings.json` are non-sensitive and can be baked into the Docker image.

#### If secrets are needed in the future

Use `fly secrets set` to inject sensitive values as environment variables:

```bash
fly secrets set SOME_API_KEY="value123"
```

ASP.NET Core automatically reads environment variables. For nested config (e.g., `OsrsApi:ApiKey`), use the double-underscore convention:

```bash
fly secrets set OsrsApi__ApiKey="value123"
```

This maps to `Configuration["OsrsApi:ApiKey"]` in code.

#### Overriding non-secret config per environment

Add values to `[env]` in `fly.toml`:

```toml
[env]
  OsrsApi__UserAgent = "OSRSProfitCalculatorV2/1.0 - Fly.io Hosted"
```

### Step 5: Optional -- Add a Dedicated Health Check Endpoint

While the home page `/` works as a basic health check, a dedicated endpoint is better practice because it:
- Is lightweight (no view rendering)
- Can check downstream dependencies (e.g., verify the OSRS API is reachable)
- Returns standard HTTP status codes

Add to `Program.cs` after `app.MapControllerRoute(...)`:

```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
```

Then update `fly.toml`:

```toml
[[http_service.checks]]
  path = "/health"
```

### Step 6: Deployment Steps

#### Prerequisites

1. Install flyctl CLI: https://fly.io/docs/flyctl/install/
2. Authenticate: `fly auth login`
3. Docker must be available (flyctl can also use Fly.io's remote builders with `--remote-only`)

#### First-time deployment

```bash
# Navigate to repo root
cd C:\Users\vince\OneDrive\Documents\Development\OSRSProfitCalculatorV2

# Option A: Interactive setup (creates app, asks about region, etc.)
fly launch --no-deploy

# Option B: If fly.toml is already checked in, just deploy directly
fly deploy

# Check deployment status
fly status

# View logs
fly logs

# Open the app in browser
fly open
```

#### Subsequent deployments

```bash
fly deploy
```

#### Useful commands

```bash
# View application status
fly status

# View logs (streaming)
fly logs

# SSH into the running machine
fly ssh console

# Scale to 2 machines
fly scale count 2

# Change VM size (256MB is fine for this app)
fly scale vm shared-cpu-1x --memory 256

# View secrets
fly secrets list

# Restart the app
fly apps restart osrs-profit-calculator
```

### Step 7: Optional -- GitHub Actions CI/CD

Create `.github/workflows/fly-deploy.yml` for automatic deployment on push to main:

```yaml
name: Deploy to Fly.io

on:
  push:
    branches: [main]

jobs:
  deploy:
    name: Deploy
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: superfly/flyctl-actions/setup-flyctl@master

      - run: flyctl deploy --remote-only
        env:
          FLY_API_TOKEN: ${{ secrets.FLY_API_TOKEN }}
```

To set up:
1. Generate a deploy token: `fly tokens create deploy -x 999999h`
2. Add it as a GitHub Actions secret named `FLY_API_TOKEN`

---

## HTTPS Redirect Loop Prevention (Critical)

The current `Program.cs` has:

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
```

Behind Fly.io's proxy, the app receives plain HTTP on port 8080. Without forwarded headers support, `UseHttpsRedirection()` would see all requests as HTTP and redirect indefinitely.

Setting `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` in `fly.toml` `[env]` solves this. ASP.NET Core 8 natively supports this environment variable -- it automatically configures `ForwardedHeadersMiddleware` to trust `X-Forwarded-Proto: https` from the Fly.io proxy. No code changes required.

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| HTTPS redirect loop behind proxy | App unusable (infinite redirects) | Set `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` in fly.toml |
| Cold start latency (auto_stop) | 2-5s delay for first request after idle | Set `min_machines_running = 1` if unacceptable; costs ~$2-5/month |
| Docker build fails due to NuGet restore | Deployment blocked | Ensure `dotnet restore` has network access during build; use `--remote-only` for Fly.io builders |
| Rate limiting by OSRS Wiki API | App returns errors under heavy use | Cache durations already configured; consider longer cache for production |
| Image size bloat | Slow deploys | Multi-stage build keeps runtime image small (~220MB) |

---

## Cost Estimate (Fly.io)

For a low-traffic personal project:

| Resource | Config | Estimated Cost |
|---|---|---|
| Shared CPU 1x, 256MB | 1 machine, auto-stop | ~$0-2/month (free tier covers this) |
| Outbound bandwidth | Included up to limits | $0 |
| **Total** | | **$0-2/month** |

Fly.io's free tier includes 3 shared-cpu-1x VMs with 256MB RAM each, which is more than sufficient for this app.

---

## Token Estimates

| Step | Description | Token Est. Low | Token Est. High |
|---|---|---|---|
| 1 | Create Dockerfile | 10,000 | 15,000 |
| 2 | Create .dockerignore | 10,000 | 10,000 |
| 3 | Create fly.toml | 10,000 | 15,000 |
| 4 | Add health check endpoint (optional) | 10,000 | 20,000 |
| 5 | First deployment + debugging | 15,000 | 30,000 |
| 6 | GitHub Actions workflow (optional) | 10,000 | 20,000 |
| **Total** | | **65,000** | **110,000** |

If steps 1-3 are done as a single task: **25,000 - 40,000 tokens** (including tester + reviewer overhead).

---

## TODO Checklist

- [ ] Create `Dockerfile` at repo root (multi-stage build for net8.0)
- [ ] Create `.dockerignore` at repo root
- [ ] Create `fly.toml` at repo root with proper configuration
- [ ] Update `.gitignore` to exclude Fly.io local state files if needed
- [ ] (Optional) Add `/health` endpoint to `Program.cs`
- [ ] (Optional) Add health check unit test
- [ ] Install flyctl CLI
- [ ] Run `fly auth login`
- [ ] Run `fly deploy` for first deployment
- [ ] Verify app is accessible via Fly.io URL
- [ ] Verify no HTTPS redirect loops
- [ ] Verify OSRS Wiki API calls work from Fly.io
- [ ] (Optional) Set up GitHub Actions deployment workflow
- [ ] (Optional) Add `FLY_API_TOKEN` to GitHub repository secrets

---

## References

- [Fly.io .NET Documentation](https://fly.io/docs/languages-and-frameworks/dotnet/)
- [Fly.io fly.toml Configuration Reference](https://fly.io/docs/reference/configuration/)
- [Fly.io Health Checks](https://fly.io/docs/reference/health-checks/)
- [Fly.io Secrets Management](https://fly.io/docs/apps/secrets/)
- [Fly.io Autostop/Autostart](https://fly.io/docs/launch/autostop-autostart/)
- [Deploy with a Dockerfile on Fly.io](https://fly.io/docs/languages-and-frameworks/dockerfile/)
- [Microsoft: Docker Images for ASP.NET Core 8](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-8.0)
- [Microsoft: Containerize a .NET App](https://learn.microsoft.com/en-us/dotnet/core/docker/build-container)
