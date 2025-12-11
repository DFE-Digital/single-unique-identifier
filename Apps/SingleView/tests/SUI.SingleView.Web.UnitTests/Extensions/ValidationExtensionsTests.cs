using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Shouldly;
using SUI.SingleView.Domain.Models;
using SUI.SingleView.Web.Extensions;

namespace SUI.SingleView.Web.UnitTests.Extensions;

public class ValidationExtensionsTests
{
    [Fact]
    public void AddToModelState_AddsErrorsWithAndWithoutPrefix()
    {
        // Arrange
        var result = new ValidationResult([
            new ValidationFailure("Field1", "Error 1"),
            new ValidationFailure("Field2", "Error 2"),
        ]);
        var modelState = new ModelStateDictionary();

        // Act
        result.AddToModelState(modelState);
        result.AddToModelState(modelState, prefix: "Prefix");

        // Assert
        modelState.ShouldContainKey("Field1");
        modelState["Field1"]!.Errors[0].ErrorMessage.ShouldBe("Error 1");

        modelState.ShouldContainKey("Field2");
        modelState["Field2"]!.Errors[0].ErrorMessage.ShouldBe("Error 2");

        modelState.ShouldContainKey("Prefix.Field1");
        modelState["Prefix.Field1"]!.Errors[0].ErrorMessage.ShouldBe("Error 1");

        modelState.ShouldContainKey("Prefix.Field2");
        modelState["Prefix.Field2"]!.Errors[0].ErrorMessage.ShouldBe("Error 2");
    }

    [Theory]
    [InlineData("111 111 1111", true)]
    [InlineData("1111111111", true)]
    [InlineData("foo", false)]
    [InlineData(null, false)]
    public void IsValidNhsNumber_UsesDomainValidation(string? nhsNumber, bool expected)
    {
        // Act
        var result = NhsNumber.IsValid(nhsNumber);

        // Assert
        result.ShouldBe(expected);
    }
}
