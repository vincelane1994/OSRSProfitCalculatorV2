using Microsoft.Extensions.Caching.Memory;
using OSRSTools.Infrastructure.Caching;
using Xunit;

namespace OSRSTools.UnitTests.Infrastructure;

public class MemoryCacheServiceTests
{
    private readonly MemoryCacheService _sut;

    public MemoryCacheServiceTests()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        _sut = new MemoryCacheService(memoryCache);
    }

    [Fact]
    public void Set_And_Get_ReturnsStoredValue()
    {
        // Arrange
        const string key = "test-key";
        const string value = "test-value";

        // Act
        _sut.Set(key, value, TimeSpan.FromMinutes(5));
        var result = _sut.Get<string>(key);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void Get_NonExistentKey_ReturnsNull()
    {
        // Act
        var result = _sut.Get<string>("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryGet_ExistingKey_ReturnsTrueAndValue()
    {
        // Arrange
        const string key = "try-key";
        const int value = 42;
        _sut.Set(key, value, TimeSpan.FromMinutes(5));

        // Act
        var found = _sut.TryGet<int>(key, out var result);

        // Assert
        Assert.True(found);
        Assert.Equal(value, result);
    }

    [Fact]
    public void TryGet_NonExistentKey_ReturnsFalse()
    {
        // Act
        var found = _sut.TryGet<string>("missing", out var result);

        // Assert
        Assert.False(found);
    }

    [Fact]
    public void Remove_ExistingKey_RemovesFromCache()
    {
        // Arrange
        const string key = "remove-key";
        _sut.Set(key, "value", TimeSpan.FromMinutes(5));

        // Act
        _sut.Remove(key);
        var result = _sut.Get<string>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Set_OverwritesExistingValue()
    {
        // Arrange
        const string key = "overwrite-key";
        _sut.Set(key, "old-value", TimeSpan.FromMinutes(5));

        // Act
        _sut.Set(key, "new-value", TimeSpan.FromMinutes(5));
        var result = _sut.Get<string>(key);

        // Assert
        Assert.Equal("new-value", result);
    }
}
