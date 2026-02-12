using Microsoft.AspNetCore.Mvc;
using Moq;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;
using OSRSTools.Web.Controllers;
using OSRSTools.Web.ViewModels;

namespace OSRSTools.UnitTests.Web.Controllers;

public class HighAlchingControllerTests
{
    private readonly Mock<IHighAlchingService> _serviceMock = new();
    private readonly HighAlchingController _sut;

    public HighAlchingControllerTests()
    {
        _sut = new HighAlchingController(_serviceMock.Object);
    }

    #region Index

    [Fact]
    public async Task Index_WithItems_ReturnsViewResultWithModel()
    {
        // Arrange
        var items = new List<HighAlchItem>
        {
            new() { ItemId = 1, Name = "Rune Platebody", Profit = 880, BuyPrice = 38000, HighAlchValue = 39000 },
            new() { ItemId = 2, Name = "Rune Platelegs", Profit = 500, BuyPrice = 37000, HighAlchValue = 38000 }
        };

        _serviceMock.Setup(x => x.GetProfitableItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        // Act
        var result = await _sut.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<HighAlchViewModel>(viewResult.Model);
        Assert.Equal(2, model.Items.Count);
        Assert.Equal("Rune Platebody", model.Items[0].Name);
        Assert.Null(model.ErrorMessage);
    }

    [Fact]
    public async Task Index_ServiceThrows_ReturnsViewWithErrorMessage()
    {
        // Arrange
        _serviceMock.Setup(x => x.GetProfitableItemsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API unavailable"));

        // Act
        var result = await _sut.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<HighAlchViewModel>(viewResult.Model);
        Assert.Empty(model.Items);
        Assert.NotNull(model.ErrorMessage);
        Assert.Contains("Failed to load", model.ErrorMessage);
    }

    [Fact]
    public async Task Index_EmptyResults_ReturnsViewWithEmptyList()
    {
        // Arrange
        _serviceMock.Setup(x => x.GetProfitableItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HighAlchItem>());

        // Act
        var result = await _sut.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<HighAlchViewModel>(viewResult.Model);
        Assert.Empty(model.Items);
        Assert.Equal(0, model.TotalItems);
        Assert.Equal(0, model.ProfitableItems);
        Assert.Null(model.ErrorMessage);
    }

    #endregion
}
