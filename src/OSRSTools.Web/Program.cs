using OSRSTools.Core.Configuration;
using OSRSTools.Core.Interfaces;
using OSRSTools.Infrastructure.Caching;

var builder = WebApplication.CreateBuilder(args);

// Configuration binding
builder.Services.Configure<OsrsApiSettings>(
    builder.Configuration.GetSection("OsrsApi"));
builder.Services.Configure<TaxSettings>(
    builder.Configuration.GetSection("Tax"));
builder.Services.Configure<CacheSettings>(
    builder.Configuration.GetSection("Cache"));
builder.Services.Configure<GoogleSheetsSettings>(
    builder.Configuration.GetSection("GoogleSheets"));
builder.Services.Configure<PriceWeightSettings>(
    builder.Configuration.GetSection("PriceWeights"));

// Core services
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

// Infrastructure
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

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
