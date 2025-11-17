using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SUI.SingleView.Application.Services;
using SUI.SingleView.Domain.Models;
using SUI.SingleView.Web.Extensions;

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

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await validator.ValidateAsync(this);
        if (!result.IsValid)
        {
            result.AddToModelState(ModelState);
            return Page();
        }

        if (!string.IsNullOrEmpty(NhsNumber))
            Results = searchService.Search(NhsNumber);
        else
            Results = searchService.Search(
                FirstName,
                LastName,
                DateOfBirth,
                HouseNumberOrName,
                Postcode
            );

        return Page();
    }
}
