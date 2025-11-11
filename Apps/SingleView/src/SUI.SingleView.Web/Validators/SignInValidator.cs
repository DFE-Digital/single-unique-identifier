using FluentValidation;
using SUI.SingleView.Web.Pages;

namespace SUI.SingleView.Web.Validators;

public class SignInValidator : AbstractValidator<SignIn>
{
    public SignInValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Username).NotEmpty().WithMessage("Enter your username");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Enter your password");
    }
}
