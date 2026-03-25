using Microsoft.AspNetCore.Mvc;
using Moq;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;
using OSRSTools.Web.Controllers;
using OSRSTools.Web.ViewModels;

namespace OSRSTools.UnitTests.Web.Controllers;

public class HerbloreControllerTests
{
    private readonly Mock<IHerbloreService> _herbloreServiceMock = new();
    private readonly HerbloreController _sut;

    public HerbloreControllerTests()
    {
        _sut = new HerbloreController(_herbloreServiceMock.Object);
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithHerbloreViewModel()
    {
        // Arrange
        _herbloreServiceMock
            .Setup(x => x.GetCleaningProfitsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HerbloreItem>());
        _herbloreServiceMock
            .Setup(x => x.GetFullProcessProfitsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HerbloreItem>());
        _herbloreServiceMock
            .Setup(x => x.GetPotionMakingProfitsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HerbloreItem>());

        // Act
        var result = await _sut.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<HerbloreViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task Index_PopulatesAllThreeLists()
    {
        // Arrange
        var cleaningItems = new List<HerbloreItem>
        {
            new() { HerbName = "Ranarr weed", Method = HerbloreMethod.Cleaning, ProfitPerUnit = 500, Members = true }
        };
        var fullProcessItems = new List<HerbloreItem>
        {
            new() { HerbName = "Ranarr weed", Method = HerbloreMethod.FullProcess, ProfitPerUnit = 200, Members = true }
        };
        var potionItems = new List<HerbloreItem>
        {
            new() { HerbName = "Ranarr weed", Method = HerbloreMethod.PotionMaking, ProfitPerUnit = 3600, Members = true }
        };

        _herbloreServiceMock
            .Setup(x => x.GetCleaningProfitsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cleaningItems);
        _herbloreServiceMock
            .Setup(x => x.GetFullProcessProfitsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(fullProcessItems);
        _herbloreServiceMock
            .Setup(x => x.GetPotionMakingProfitsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(potionItems);

        // Act
        var result = await _sut.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var viewModel  = Assert.IsType<HerbloreViewModel>(viewResult.Model);
        Assert.Single(viewModel.CleaningItems);
        Assert.Single(viewModel.FullProcessItems);
        Assert.Single(viewModel.PotionMakingItems);
    }

    [Fact]
    public async Task Index_ServiceThrowsException_ReturnsViewWithErrorMessage()
    {
        // Arrange
        _herbloreServiceMock
            .Setup(x => x.GetCleaningProfitsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API unavailable"));
        _herbloreServiceMock
            .Setup(x => x.GetFullProcessProfitsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HerbloreItem>());
        _herbloreServiceMock
            .Setup(x => x.GetPotionMakingProfitsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HerbloreItem>());

        // Act
        var result = await _sut.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var viewModel  = Assert.IsType<HerbloreViewModel>(viewResult.Model);
        Assert.NotNull(viewModel.ErrorMessage);
        Assert.Contains("Failed to load Herblore data", viewModel.ErrorMessage);
    }
}
