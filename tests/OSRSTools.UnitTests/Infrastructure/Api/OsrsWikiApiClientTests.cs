using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OSRSTools.Core.Configuration;
using OSRSTools.Core.Entities;
using OSRSTools.Infrastructure.Api;
using Xunit;

namespace OSRSTools.UnitTests.Infrastructure.Api;

public class OsrsWikiApiClientTests
{
    private readonly Mock<ILogger<OsrsWikiApiClient>> _loggerMock = new();
    private readonly OsrsApiSettings _settings = new()
    {
        BaseUrl = "https://prices.runescape.wiki/api/v1/osrs",
        UserAgent = "TestAgent/1.0",
        Endpoints = new ApiEndpointSettings()
    };

    private OsrsWikiApiClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new OsrsWikiApiClient(httpClient, Options.Create(_settings), _loggerMock.Object);
    }

    #region GetAllMappingsAsync

    [Fact]
    public async Task GetAllMappingsAsync_ValidResponse_MapsToDomainEntities()
    {
        var json = JsonSerializer.Serialize(new[]
        {
            new { id = 2, name = "Cannonball", members = false, limit = 7000, highalch = 5, examine = "Ammo", icon = "cannonball.png" },
            new { id = 4151, name = "Abyssal whip", members = true, limit = 70, highalch = 72000, examine = (string?)null, icon = (string?)null }
        });

        var client = CreateClient(new FakeHttpHandler(json));
        var result = await client.GetAllMappingsAsync();

        Assert.Equal(2, result.Count);

        Assert.Equal(2, result[2].ItemId);
        Assert.Equal("Cannonball", result[2].Name);
        Assert.False(result[2].Members);
        Assert.Equal(7000, result[2].BuyLimit);

        Assert.Equal(4151, result[4151].ItemId);
        Assert.Equal("Abyssal whip", result[4151].Name);
        Assert.True(result[4151].Members);
        Assert.Equal(70, result[4151].BuyLimit);
    }

    [Fact]
    public async Task GetAllMappingsAsync_NullLimit_DefaultsToZero()
    {
        var json = "[{\"id\":1,\"name\":\"Test\",\"members\":false,\"limit\":null}]";

        var client = CreateClient(new FakeHttpHandler(json));
        var result = await client.GetAllMappingsAsync();

        Assert.Equal(0, result[1].BuyLimit);
    }

    [Fact]
    public async Task GetAllMappingsAsync_StoresHighAlchValues()
    {
        var json = "[{\"id\":1,\"name\":\"Test\",\"members\":false,\"limit\":100,\"highalch\":500}]";

        var client = CreateClient(new FakeHttpHandler(json));
        await client.GetAllMappingsAsync();

        Assert.Equal(500, client.GetHighAlchValue(1));
    }

    [Fact]
    public async Task GetAllMappingsAsync_NullHighAlch_ReturnsNull()
    {
        var json = "[{\"id\":1,\"name\":\"Test\",\"members\":false,\"limit\":100,\"highalch\":null}]";

        var client = CreateClient(new FakeHttpHandler(json));
        await client.GetAllMappingsAsync();

        Assert.Null(client.GetHighAlchValue(1));
    }

    [Fact]
    public async Task GetAllMappingsAsync_EmptyResponse_ReturnsEmptyDictionary()
    {
        var client = CreateClient(new FakeHttpHandler("[]"));
        var result = await client.GetAllMappingsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllMappingsAsync_EmptyName_SkipsItem()
    {
        var json = JsonSerializer.Serialize(new[]
        {
            new { id = 1, name = "", members = false, limit = 100, highalch = 50 },
            new { id = 2, name = "Valid Item", members = false, limit = 200, highalch = 100 }
        });

        var client = CreateClient(new FakeHttpHandler(json));
        var result = await client.GetAllMappingsAsync();

        Assert.Single(result);
        Assert.True(result.ContainsKey(2));
        Assert.False(result.ContainsKey(1));
    }

    [Fact]
    public async Task GetAllMappingsAsync_NegativeHighAlch_TreatedAsNull()
    {
        var json = "[{\"id\":1,\"name\":\"Test\",\"members\":false,\"limit\":100,\"highalch\":-5}]";

        var client = CreateClient(new FakeHttpHandler(json));
        await client.GetAllMappingsAsync();

        Assert.Null(client.GetHighAlchValue(1));
    }

    #endregion

    #region GetLatestPricesAsync

    [Fact]
    public async Task GetLatestPricesAsync_ValidResponse_MapsPricesCorrectly()
    {
        var json = """
        {
            "data": {
                "2": { "high": 150, "highTime": 1707000000, "low": 145, "lowTime": 1707000001 },
                "4151": { "high": 2500000, "highTime": 1707000002, "low": 2400000, "lowTime": 1707000003 }
            }
        }
        """;

        var client = CreateClient(new FakeHttpHandler(json));
        var result = await client.GetLatestPricesAsync();

        Assert.Equal(2, result.Count);

        Assert.Equal(150, result[2].LatestBuyPrice);
        Assert.Equal(145, result[2].LatestSellPrice);
        Assert.NotNull(result[2].LatestBuyTime);
        Assert.NotNull(result[2].LatestSellTime);

        Assert.Equal(2500000, result[4151].LatestBuyPrice);
        Assert.Equal(2400000, result[4151].LatestSellPrice);
    }

    [Fact]
    public async Task GetLatestPricesAsync_NullPrices_MapsAsNull()
    {
        var json = """
        {
            "data": {
                "1": { "high": null, "highTime": null, "low": null, "lowTime": null }
            }
        }
        """;

        var client = CreateClient(new FakeHttpHandler(json));
        var result = await client.GetLatestPricesAsync();

        Assert.Null(result[1].LatestBuyPrice);
        Assert.Null(result[1].LatestSellPrice);
        Assert.Null(result[1].LatestBuyTime);
        Assert.Null(result[1].LatestSellTime);
    }

    [Fact]
    public async Task GetLatestPricesAsync_EmptyData_ReturnsEmptyDictionary()
    {
        var json = """{"data": {}}""";

        var client = CreateClient(new FakeHttpHandler(json));
        var result = await client.GetLatestPricesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLatestPricesAsync_NonNumericItemId_SkipsItem()
    {
        var json = """
        {
            "data": {
                "abc": { "high": 100, "highTime": 1707000000, "low": 95, "lowTime": 1707000001 },
                "2": { "high": 150, "highTime": 1707000000, "low": 145, "lowTime": 1707000001 }
            }
        }
        """;

        var client = CreateClient(new FakeHttpHandler(json));
        var result = await client.GetLatestPricesAsync();

        Assert.Single(result);
        Assert.True(result.ContainsKey(2));
    }

    [Fact]
    public async Task GetLatestPricesAsync_NegativePrice_SkipsItem()
    {
        var json = """
        {
            "data": {
                "1": { "high": -100, "highTime": 1707000000, "low": 95, "lowTime": 1707000001 },
                "2": { "high": 150, "highTime": 1707000000, "low": 145, "lowTime": 1707000001 }
            }
        }
        """;

        var client = CreateClient(new FakeHttpHandler(json));
        var result = await client.GetLatestPricesAsync();

        Assert.Single(result);
        Assert.True(result.ContainsKey(2));
        Assert.False(result.ContainsKey(1));
    }

    #endregion

    #region GetTimeWindowPricesAsync

    [Fact]
    public async Task GetTimeWindowPricesAsync_ValidResponse_MapsCorrectly()
    {
        var json = """
        {
            "data": {
                "2": { "avgHighPrice": 148, "highPriceVolume": 50000, "avgLowPrice": 147, "lowPriceVolume": 48000 }
            },
            "timestamp": 1707000000
        }
        """;

        var client = CreateClient(new FakeHttpHandler(json));
        var result = await client.GetTimeWindowPricesAsync(TimeWindow.FiveMinute);

        Assert.Single(result);
        Assert.Equal(148, result[2].AvgBuyPrice);
        Assert.Equal(147, result[2].AvgSellPrice);
        Assert.Equal(50000, result[2].BuyVolume);
        Assert.Equal(48000, result[2].SellVolume);
    }

    [Fact]
    public async Task GetTimeWindowPricesAsync_NullPrices_MapsAsNull()
    {
        var json = """
        {
            "data": {
                "1": { "avgHighPrice": null, "highPriceVolume": null, "avgLowPrice": null, "lowPriceVolume": null }
            },
            "timestamp": 1707000000
        }
        """;

        var client = CreateClient(new FakeHttpHandler(json));
        var result = await client.GetTimeWindowPricesAsync(TimeWindow.OneHour);

        Assert.Null(result[1].AvgBuyPrice);
        Assert.Null(result[1].AvgSellPrice);
        Assert.Null(result[1].BuyVolume);
        Assert.Null(result[1].SellVolume);
    }

    [Theory]
    [InlineData(TimeWindow.FiveMinute)]
    [InlineData(TimeWindow.OneHour)]
    [InlineData(TimeWindow.SixHour)]
    [InlineData(TimeWindow.TwentyFourHour)]
    public async Task GetTimeWindowPricesAsync_AllWindows_ReturnValidResults(TimeWindow window)
    {
        var json = """{"data": {"1": {"avgHighPrice": 100, "highPriceVolume": 1000, "avgLowPrice": 95, "lowPriceVolume": 900}}, "timestamp": 1707000000}""";

        var client = CreateClient(new FakeHttpHandler(json));
        var result = await client.GetTimeWindowPricesAsync(window);

        Assert.Single(result);
        Assert.Equal(100, result[1].AvgBuyPrice);
    }

    [Fact]
    public async Task GetTimeWindowPricesAsync_VolumeExceedsIntMax_CapsToIntMax()
    {
        var largeVolume = (long)int.MaxValue + 1000;
        var json = $$"""
        {
            "data": {
                "1": { "avgHighPrice": 100, "highPriceVolume": {{largeVolume}}, "avgLowPrice": 95, "lowPriceVolume": {{largeVolume}} }
            },
            "timestamp": 1707000000
        }
        """;

        var client = CreateClient(new FakeHttpHandler(json));
        var result = await client.GetTimeWindowPricesAsync(TimeWindow.FiveMinute);

        Assert.Single(result);
        Assert.Equal(int.MaxValue, result[1].BuyVolume);
        Assert.Equal(int.MaxValue, result[1].SellVolume);
    }

    [Fact]
    public async Task GetTimeWindowPricesAsync_NegativePrice_SkipsItem()
    {
        var json = """
        {
            "data": {
                "1": { "avgHighPrice": -100, "highPriceVolume": 1000, "avgLowPrice": 95, "lowPriceVolume": 900 },
                "2": { "avgHighPrice": 200, "highPriceVolume": 2000, "avgLowPrice": 195, "lowPriceVolume": 1900 }
            },
            "timestamp": 1707000000
        }
        """;

        var client = CreateClient(new FakeHttpHandler(json));
        var result = await client.GetTimeWindowPricesAsync(TimeWindow.FiveMinute);

        Assert.Single(result);
        Assert.True(result.ContainsKey(2));
        Assert.False(result.ContainsKey(1));
    }

    [Fact]
    public async Task GetTimeWindowPricesAsync_NegativeVolume_SkipsItem()
    {
        var json = """
        {
            "data": {
                "1": { "avgHighPrice": 100, "highPriceVolume": -500, "avgLowPrice": 95, "lowPriceVolume": 900 },
                "2": { "avgHighPrice": 200, "highPriceVolume": 2000, "avgLowPrice": 195, "lowPriceVolume": 1900 }
            },
            "timestamp": 1707000000
        }
        """;

        var client = CreateClient(new FakeHttpHandler(json));
        var result = await client.GetTimeWindowPricesAsync(TimeWindow.FiveMinute);

        Assert.Single(result);
        Assert.True(result.ContainsKey(2));
        Assert.False(result.ContainsKey(1));
    }

    [Fact]
    public async Task GetTimeWindowPricesAsync_NonNumericItemId_SkipsItem()
    {
        var json = """
        {
            "data": {
                "abc": { "avgHighPrice": 100, "highPriceVolume": 1000, "avgLowPrice": 95, "lowPriceVolume": 900 },
                "2": { "avgHighPrice": 200, "highPriceVolume": 2000, "avgLowPrice": 195, "lowPriceVolume": 1900 }
            },
            "timestamp": 1707000000
        }
        """;

        var client = CreateClient(new FakeHttpHandler(json));
        var result = await client.GetTimeWindowPricesAsync(TimeWindow.FiveMinute);

        Assert.Single(result);
        Assert.True(result.ContainsKey(2));
    }

    #endregion

    #region GetHighAlchValue

    [Fact]
    public void GetHighAlchValue_NotLoaded_ReturnsNull()
    {
        var client = CreateClient(new FakeHttpHandler("[]"));

        Assert.Null(client.GetHighAlchValue(999));
    }

    #endregion

    /// <summary>
    /// Simple fake HTTP handler that returns a preset JSON response.
    /// </summary>
    private class FakeHttpHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public FakeHttpHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
