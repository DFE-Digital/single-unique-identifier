using System.Text.RegularExpressions;
using FluentValidation;
using SUI.SingleView.Web.Extensions;
using SUI.SingleView.Web.Pages;

namespace SUI.SingleView.Web.Validators;

public class SearchValidator : AbstractValidator<Search>
{
    public SearchValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x)
            .Must(HasAtLeastOneField)
            .WithMessage("Enter at least one piece of information");

        RuleFor(x => x.NhsNumber)
            .IsValidNhsNumber()
            .WithMessage("Enter a valid 10-digit NHS number")
            .When(x => !string.IsNullOrWhiteSpace(x.NhsNumber));
    }

    private static bool HasAtLeastOneField(Search model)
    {
        return !string.IsNullOrWhiteSpace(model.FirstName)
            || !string.IsNullOrWhiteSpace(model.LastName)
            || model.DateOfBirth is not null
            || !string.IsNullOrWhiteSpace(model.HouseNumberOrName)
            || !string.IsNullOrWhiteSpace(model.Postcode)
            || !string.IsNullOrWhiteSpace(model.NhsNumber);
    }
}
