using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shouldly;
using SUI.SingleView.Web.Pages;
using SUI.SingleView.Web.Validators;

namespace SUI.SingleView.Web.UnitTests.Pages;

public class SignInTests : PageModelTestBase<SignIn>
{
    private readonly IValidator<SignIn> _validator = new SignInValidator();

    [Fact]
    public void OnGet_ReturnsPage()
    {
        var sut = new SignIn(MockLogger, _validator);

        // Act
        var result = sut.OnGet();

        // Assert
        result.ShouldBeOfType<PageResult>();
    }

    [Fact]
    public async Task OnPost_RedirectsToSearchOnSuccess()
    {
        var sut = new SignIn(MockLogger, _validator) { Username = "user", Password = "pass" };

        // Act
        var result = await sut.OnPostAsync();

        // Assert
        result.ShouldBeOfType<RedirectToPageResult>();
        ((RedirectToPageResult)result).PageName.ShouldBe("/Search");
    }

    [Fact]
    public async Task OnPost_ReturnsErrorOnEmptyInput()
    {
        var sut = new SignIn(MockLogger, _validator);

        // Act
        var result = await sut.OnPostAsync();

        // Assert
        result.ShouldBeOfType<PageResult>();

        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState.ErrorCount.ShouldBe(2);
        sut.ModelState[nameof(SignIn.Username)]!.Errors.Count.ShouldBe(1);
        sut.ModelState[nameof(SignIn.Username)]!
            .Errors[0]
            .ErrorMessage.ShouldBe("Enter your username");
        sut.ModelState[nameof(SignIn.Password)]!.Errors.Count.ShouldBe(1);
        sut.ModelState[nameof(SignIn.Password)]!
            .Errors[0]
            .ErrorMessage.ShouldBe("Enter your password");
    }

    [Fact]
    public async Task OnPost_ReturnsErrorOnFailedSignIn()
    {
        var sut = new SignIn(MockLogger, _validator) { Username = "foo", Password = "bar" };

        // Act
        var result = await sut.OnPostAsync();

        // Assert
        result.ShouldBeOfType<PageResult>();

        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState.ErrorCount.ShouldBe(2);
        sut.ModelState[nameof(SignIn.Username)]!.Errors.Count.ShouldBe(1);
        sut.ModelState[nameof(SignIn.Username)]!
            .Errors[0]
            .ErrorMessage.ShouldBe("Your username or password is incorrect");
        sut.ModelState[nameof(SignIn.Password)]!.Errors.Count.ShouldBe(1);
        sut.ModelState[nameof(SignIn.Password)]!
            .Errors[0]
            .ErrorMessage.ShouldBe("Your username or password is incorrect");
    }
}
