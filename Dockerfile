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
