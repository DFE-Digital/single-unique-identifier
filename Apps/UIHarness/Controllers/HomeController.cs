using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UIHarness.Interfaces;
using UIHarness.Models;

namespace UIHarness.Controllers;

[Authorize]
public sealed class HomeController(IPersonRepository repo) : Controller
{
    private readonly IPersonRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));

    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var people = await _repo.GetAllAsync(cancellationToken);

        var rows = people.Select(p => new PersonRowVm
        {
            PersonId = p.PersonId,
            Given = p.Given,
            Family = p.Family,
            Birthdate = p.Birthdate,
            Gender = p.Gender,
            Phone = p.Phone,
            Email = p.Email,
            Postcode = p.Postcode,
            NhsNumber = null
        }).ToList();

        return View(rows);
    }
}
