using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using Shouldly;
using SUI.SingleView.Application.Models;
using SUI.SingleView.Application.Services;
using SUI.SingleView.Web.Pages;
using SUI.SingleView.Web.Validators;

namespace SUI.SingleView.Web.UnitTests.Pages;

public class DataViewTests : PageModelTestBase<DataView>
{
    private readonly IRecordService _mockRecordService = Substitute.For<IRecordService>();

    [Fact]
    public async Task OnGet_ReturnsPage()
    {
        var sut = new DataView(MockLogger, _mockRecordService);
        _mockRecordService.GetRecordAsync("1234567890").Returns(new PersonModel());

        // Act
        await sut.OnGetAsync();

        // Assert
        sut.PersonModel.ShouldBeOfType<PersonModel>();
    }
}
