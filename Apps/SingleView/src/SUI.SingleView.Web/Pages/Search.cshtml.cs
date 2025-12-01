using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SUI.SingleView.Application.Services;
using SUI.SingleView.Domain.Models;
using SUI.SingleView.Web.Extensions;
using SUI.SingleView.Web.Models;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace SUI.SingleView.Web.Pages;

public class Search(
    ILogger<Search> logger,
    IValidator<Search> validator,
    ISearchService searchService
) : PageModel
{
    private readonly ILogger<Search> _logger = logger;

    [BindProperty, Display(Name = "NHS Number")]
    public string? NhsNumber { get; set; }

    [BindProperty, Display(Name = "First name")]
    public string? FirstName { get; set; }

    [BindProperty, Display(Name = "Last name")]
    public string? LastName { get; set; }

    [BindProperty, Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [BindProperty, Display(Name = "House number or name")]
    public string? HouseNumberOrName { get; set; }

    [BindProperty, Display(Name = "Postcode")]
    public string? Postcode { get; set; }

    public IList<SearchResult>? Results { get; private set; }

    public bool HasResults => Results is not null;

    public PageResult OnGet()
    {
        Results = null;
        return Page();
    }

    /// <summary>
    /// Handles the standard non-AJAX POST: validate, run search, and re-render with errors or results.
    /// </summary>
    public async Task<IActionResult> OnPostAsync() =>
        await HandleSearchAsync(
            onValidationFailure: validation =>
            {
                validation.AddToModelState(ModelState);
                return Page();
            },
            onSuccess: results =>
            {
                Results = results;
                return Page();
            }
        );

    /// <summary>
    /// Handles the AJAX POST for progressive enhancement: returns JSON for client-side update without a full reload.
    /// </summary>
    public async Task<IActionResult> OnPostSearchAsync() =>
        await HandleSearchAsync(
            onValidationFailure: validation => new JsonResult(
                new SearchApiResponse
                {
                    Success = false,
                    ErrorMessage = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)),
                }
            ),
            onSuccess: results => new JsonResult(
                new SearchApiResponse { Success = true, Results = results }
            ),
            onError: ex =>
            {
                _logger.LogError(ex, "Error occurred during search");
                return new JsonResult(
                    new SearchApiResponse
                    {
                        Success = false,
                        ErrorMessage = "An error occurred while searching. Please try again.",
                    }
                );
            }
        );

    private async Task<IActionResult> HandleSearchAsync(
        Func<ValidationResult, IActionResult> onValidationFailure,
        Func<IList<SearchResult>, IActionResult> onSuccess,
        Func<Exception, IActionResult>? onError = null
    )
    {
        try
        {
            var (validationResult, results) = await ValidateAndSearchAsync();

            return !validationResult.IsValid
                ? onValidationFailure(validationResult)
                : onSuccess(results!);
        }
        catch (Exception ex)
        {
            if (onError is not null)
            {
                return onError(ex);
            }

            throw;
        }
    }

    private async Task<(
        ValidationResult ValidationResult,
        IList<SearchResult>? Results
    )> ValidateAndSearchAsync()
    {
        var validationResult = await validator.ValidateAsync(this);
        if (!validationResult.IsValid)
        {
            return (validationResult, null);
        }

        IList<SearchResult> results;
        if (!string.IsNullOrEmpty(NhsNumber))
        {
            results = await searchService.SearchAsync(NhsNumber);
        }
        else
        {
            results = await searchService.SearchAsync(
                FirstName,
                LastName,
                DateOfBirth,
                HouseNumberOrName,
                Postcode
            );
        }

        return (validationResult, results);
    }
}
