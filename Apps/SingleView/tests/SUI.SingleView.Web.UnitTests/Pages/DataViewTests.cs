using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shouldly;
using SUI.SingleView.Application.Models;
using SUI.SingleView.Web.Pages;
using SUI.SingleView.Web.Validators;

namespace SUI.SingleView.Web.UnitTests.Pages;

public class DataViewTests : PageModelTestBase<DataView>
{
    [Fact]
    public void OnGet_ReturnsPage()
    {
        var sut = new DataView(MockLogger);

        // Act
        sut.OnGet();

        // Assert
        sut.PersonModel.ShouldBeOfType<PersonModel>();
    }
}
