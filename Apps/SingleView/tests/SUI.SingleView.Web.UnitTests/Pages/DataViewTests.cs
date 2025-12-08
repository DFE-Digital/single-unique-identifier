using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using Shouldly;
using SUI.SingleView.Application.Models;
using SUI.SingleView.Application.Services;
using SUI.SingleView.Web.Pages;

namespace SUI.SingleView.Web.UnitTests.Pages;

public class DataViewTests : PageModelTestBase<DataView>
{
    private readonly IRecordService _mockRecordService = Substitute.For<IRecordService>();

    [Fact]
    public async Task OnGet_WithPrefetch_SetsPersonModel()
    {
        var sut = new DataView(MockLogger, _mockRecordService);
        const string nhsNumber = "1234567890";
        sut.PersonId = nhsNumber;
        _mockRecordService
            .GetRecordAsync(nhsNumber, TestContext.Current.CancellationToken)
            .Returns(new PersonModel());

        // Act
        var result = await sut.OnGetAsync(true, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeOfType<PageResult>();
        await _mockRecordService
            .Received(1)
            .GetRecordAsync(nhsNumber, TestContext.Current.CancellationToken);
        sut.PersonModel.ShouldBeOfType<PersonModel>();
    }

    [Fact]
    public async Task OnGet_WithPrefetchFalse_ReturnsPage()
    {
        var sut = new DataView(MockLogger, _mockRecordService);
        const string nhsNumber = "1234567890";
        sut.PersonId = nhsNumber;
        _mockRecordService
            .GetRecordAsync(nhsNumber, TestContext.Current.CancellationToken)
            .Returns(new PersonModel());

        // Act
        var result = await sut.OnGetAsync(false, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeOfType<PageResult>();
        await _mockRecordService
            .DidNotReceiveWithAnyArgs()
            .GetRecordAsync(nhsNumber, Arg.Any<CancellationToken>());
        sut.PersonModel.ShouldBeNull();
    }
}
