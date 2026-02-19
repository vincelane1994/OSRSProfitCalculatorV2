using Polly;
using Polly.Extensions.Http;
using OSRSTools.Core.Configuration;
using OSRSTools.Core.Interfaces;
using OSRSTools.Core.Services;
using OSRSTools.Infrastructure.Api;
using OSRSTools.Infrastructure.Caching;
using OSRSTools.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration binding
builder.Services.Configure<OsrsApiSettings>(
    builder.Configuration.GetSection("OsrsApi"));
builder.Services.Configure<TaxSettings>(
    builder.Configuration.GetSection("Tax"));
builder.Services.Configure<CacheSettings>(
    builder.Configuration.GetSection("Cache"));
builder.Services.Configure<PriceWeightSettings>(
    builder.Configuration.GetSection("PriceWeights"));

// Core services
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

// Domain services
builder.Services.AddScoped<IProfitCalculationService, ProfitCalculationService>();
builder.Services.AddScoped<IPriceRecommendationService, PriceRecommendationService>();
builder.Services.AddScoped<IFlipCalculator, FlipCalculator>();
builder.Services.AddScoped<IHighAlchingService, HighAlchingService>();

// Infrastructure — API client
// AddHttpClient registers OsrsWikiApiClient as transient by default.
// We override with AddScoped so that all three injection points
// (concrete, IItemMappingRepository, IPriceRepository) share one instance
// per request scope — critical because _highAlchValues is populated in one
// call path and read from another.
builder.Services.AddHttpClient<OsrsWikiApiClient>()
    // Retry: 3 attempts with exponential backoff (2s, 4s, 8s).
    // Starts at 2s intentionally to avoid hammering the Wiki API on transient blips.
    .AddPolicyHandler((services, _) =>
        HttpPolicyExtensions.HandleTransientHttpError()
            .WaitAndRetryAsync(3,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, delay, attempt, _) =>
                {
                    var logger = services.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("PollyHttpRetry");
                    logger.LogWarning(
                        "Retry {Attempt} after {Delay}s due to {Reason}",
                        attempt, delay.TotalSeconds,
                        outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message);
                }))
    .AddPolicyHandler((services, _) =>
        HttpPolicyExtensions.HandleTransientHttpError()
            .CircuitBreakerAsync(5,
                TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    var logger = services.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("PollyCircuitBreaker");
                    logger.LogError(
                        "Circuit breaker opened for {Duration}s due to {Reason}",
                        duration.TotalSeconds,
                        outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message);
                },
                onReset: () =>
                {
                    var logger = services.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("PollyCircuitBreaker");
                    logger.LogInformation("Circuit breaker reset");
                }))
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)));
builder.Services.AddScoped<OsrsWikiApiClient>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient(nameof(OsrsWikiApiClient));
    return ActivatorUtilities.CreateInstance<OsrsWikiApiClient>(sp,httpClient);
});
builder.Services.AddScoped<IItemMappingRepository>(sp => sp.GetRequiredService<OsrsWikiApiClient>());
builder.Services.AddScoped<IPriceRepository>(sp => sp.GetRequiredService<OsrsWikiApiClient>());

// Infrastructure — caching and data orchestration
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<IDataFetchService, DataFetchService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
