using Bogus;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using Shouldly;
using SUI.SingleView.Application.Services;
using SUI.SingleView.Domain.UnitTests.Extensions;
using SUI.SingleView.Domain.UnitTests.Fakers;
using SUI.SingleView.Web.Pages;
using SUI.SingleView.Web.Validators;

namespace SUI.SingleView.Web.UnitTests.Pages;

public class SearchTests : PageModelTestBase<Search>
{
    private readonly IValidator<Search> _validator = new SearchValidator();

    private readonly ISearchService _mockSearchService = Substitute.For<ISearchService>();

    private readonly Faker _faker = new Faker();

    [Fact]
    public void OnGet_ReturnsPageWithHasResultsFalse()
    {
        var sut = new Search(MockLogger, _validator, _mockSearchService);

        // Act
        var result = sut.OnGet();

        // Assert
        result.ShouldBeOfType<PageResult>();
        sut.HasResults.ShouldBeFalse();
    }

    [Fact]
    public async Task OnPostAsync_WithNhsNumber_ReturnsMatchingResults()
    {
        var nhsNumber = _faker.GenerateNhsNumber();
        _mockSearchService
            .Search(nhsNumber.Value)
            .Returns(new SearchResultFaker().WithNhsNumber(nhsNumber).Generate(1));
        var sut = new Search(MockLogger, _validator, _mockSearchService) { NhsNumber = nhsNumber };

        // Act
        var result = await sut.OnPostAsync();

        // Assert
        result.ShouldBeOfType<PageResult>();
        sut.Results.ShouldNotBeNull();
        sut.Results.Count.ShouldBe(1);
        sut.Results[0].NhsNumber.ShouldBe(nhsNumber);
    }

    [Fact]
    public async Task OnPostAsync_WithName_ReturnsMatchingResults()
    {
        var firstName = _faker.Name.FirstName();
        var lastName = _faker.Name.LastName();
        var expectedName = $"{firstName} {lastName}";
        _mockSearchService
            .Search(firstName, lastName, null, null, null)
            .Returns(new SearchResultFaker().WithName(expectedName).Generate(1));
        var sut = new Search(MockLogger, _validator, _mockSearchService)
        {
            FirstName = firstName,
            LastName = lastName,
        };

        // Act
        var result = await sut.OnPostAsync();

        // Assert
        result.ShouldBeOfType<PageResult>();
        sut.HasResults.ShouldBeTrue();
        sut.Results.ShouldNotBeNull();
        sut.Results.Count.ShouldBe(1);
        sut.Results[0].Name.ShouldBe(expectedName);
    }

    [Fact]
    public async Task OnPostAsync_WithDateOfBirth_ReturnsMatchingResults()
    {
        var dateOfBirth = _faker.Date.Past(18);
        _mockSearchService
            .Search(null, null, dateOfBirth, null, null)
            .Returns(new SearchResultFaker().WithDateOfBirth(dateOfBirth).Generate(1));
        var sut = new Search(MockLogger, _validator, _mockSearchService)
        {
            DateOfBirth = dateOfBirth,
        };

        // Act
        var result = await sut.OnPostAsync();

        // Assert
        result.ShouldBeOfType<PageResult>();
        sut.HasResults.ShouldBeTrue();
        sut.Results.ShouldNotBeNull();
        sut.Results.Count.ShouldBe(1);
        sut.Results[0].DateOfBirth.ShouldBe(dateOfBirth);
    }

    [Fact]
    public async Task OnPostAsync_WithAddress_ReturnsMatchingResults()
    {
        var address = new AddressFaker().Generate();
        _mockSearchService
            .Search(null, null, null, address.AddressLine1, address.Postcode)
            .Returns(new SearchResultFaker().WithAddress(address).Generate(1));
        var sut = new Search(MockLogger, _validator, _mockSearchService)
        {
            HouseNumberOrName = address.AddressLine1,
            Postcode = address.Postcode,
        };

        // Act
        var result = await sut.OnPostAsync();

        // Assert
        result.ShouldBeOfType<PageResult>();
        sut.HasResults.ShouldBeTrue();
        sut.Results.ShouldNotBeNull();
        sut.Results.Count.ShouldBe(1);
        sut.Results[0].Address.ShouldBe(address);
    }

    [Fact]
    public async Task OnPostAsync_WithBlankForm_ReturnsError()
    {
        var sut = new Search(MockLogger, _validator, _mockSearchService);

        // Act
        var result = await sut.OnPostAsync();

        // Assert
        result.ShouldBeOfType<PageResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState.ErrorCount.ShouldBe(1);
        sut.ModelState[""]!.Errors.Count.ShouldBe(1);
        sut.ModelState[""]!
            .Errors[0]
            .ErrorMessage.ShouldBe("Enter at least one piece of information");
    }

    [Fact]
    public async Task OnPostAsync_WithInvalidNhsNumber_ReturnsError()
    {
        var sut = new Search(MockLogger, _validator, _mockSearchService) { NhsNumber = "foo" };

        // Act
        var result = await sut.OnPostAsync();

        // Assert
        result.ShouldBeOfType<PageResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState.ErrorCount.ShouldBe(1);
        sut.ModelState[nameof(sut.NhsNumber)]!.Errors.Count.ShouldBe(1);
        sut.ModelState[nameof(sut.NhsNumber)]!
            .Errors[0]
            .ErrorMessage.ShouldBe("Enter a valid 10-digit NHS number");
    }
}
