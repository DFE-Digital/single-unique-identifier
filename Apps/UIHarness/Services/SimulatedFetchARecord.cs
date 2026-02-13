using UIHarness.Interfaces;
using UIHarness.Models;

namespace UIHarness.Services;

public sealed class SimulatedFetchARecord(IRecordTemplateRepository templates) : IFetchARecord
{
    private readonly IRecordTemplateRepository _templates = templates ?? throw new ArgumentNullException(nameof(templates));
    private readonly Random _random = new();

    public async Task<FetchRecordResponse> FetchAsync(Custodian custodian, string nhsNumber, string recordType, CancellationToken cancellationToken)
    {
        var delaySeconds = _random.Next(2, 7);
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);

        var applicableContacts = custodian.Contacts
            .Where(c => c.AppliesToRecordTypes.Count == 0 || c.AppliesToRecordTypes.Contains(recordType, StringComparer.Ordinal))
            .Select(c => new ContactRecord
            {
                Name = c.Name,
                Role = c.Role,
                Telephone = c.Telephone,
                Email = c.Email,
                Address = c.Address
            })
            .ToList();

        var response = new FetchRecordResponse
        {
            CustodianId = custodian.CustodianId,
            CustodianName = custodian.Name,
            NhsNumber = nhsNumber,
            RecordType = recordType,
            Contacts = applicableContacts
        };

        var canBeExternal = custodian.Category.Equals("Health", StringComparison.OrdinalIgnoreCase)
            || recordType is "Community Notes" or "Immunisation Record" or "CAMHS Referral";

        var chooseExternal = canBeExternal && _random.NextDouble() < 0.70;

        if (chooseExternal)
        {
            response.ExternalSystem = BuildExternalLink(custodian, recordType);
            response.DataSections = [];
            response.Summary = response.ExternalSystem.Notes;
            return response;
        }

        var template = await _templates.GetByRecordTypeAsync(recordType, cancellationToken);

        if (template is null)
        {
            response.DataSections =
            [
                new RecordDataSection
                {
                    Title = "Record summary",
                    Fields =
                    [
                        new RecordField { Label = "Summary", Value = "A record summary is available for this record type in the demo dataset." }
                    ]
                }
            ];

            response.ExternalSystem = null;
            response.Summary = "A record summary is available.";
            return response;
        }

        response.DataSections = RenderTemplate(template, nhsNumber);
        response.ExternalSystem = null;
        response.Summary = template.Summary;

        if (response.Contacts.Count > 0)
        {
            response.Summary = $"{response.Summary} Contact details are provided for follow-up.";
        }

        return response;
    }

    private static ExternalSystemLink BuildExternalLink(Custodian custodian, string recordType)
    {
        if (custodian.CustodianId == "nhs-01")
        {
            var (systemName, url) = recordType switch
            {
                "Immunisation Record" => ("NHS Immunisations Portal (demo)", "https://example.nhs.uk/imms/login"),
                "CAMHS Referral" => ("CAMHS Referral System (demo)", "https://example.nhs.uk/camhs/login"),
                _ => ("Clinical system portal (demo)", "https://example.nhs.uk/portal/login")
            };

            return new ExternalSystemLink
            {
                SystemName = systemName,
                Url = url,
                Notes = "A record exists, but detailed clinical information is accessed via the custodian system by authorised users."
            };
        }

        return new ExternalSystemLink
        {
            SystemName = "Custodian case management system (demo)",
            Url = "https://example.gov.uk/case-system/login",
            Notes = "A record exists, but detailed information is accessed via the custodian system by authorised users."
        };
    }

    private List<RecordDataSection> RenderTemplate(RecordTemplate template, string nhsNumber)
    {
        var n4 = nhsNumber.Length >= 4 ? nhsNumber[^4..] : nhsNumber;
        var today = DateTime.UtcNow.Date;

        var recent = today.AddDays(-_random.Next(1, 28));
        var older = today.AddDays(-_random.Next(60, 360));
        var future = today.AddDays(_random.Next(14, 90));

        string ReplaceTokens(string s)
        {
            return s
                .Replace("{N4}", n4, StringComparison.Ordinal)
                .Replace("{RECENT_DATE}", recent.ToString("yyyy-MM-dd"), StringComparison.Ordinal)
                .Replace("{OLDER_DATE}", older.ToString("yyyy-MM-dd"), StringComparison.Ordinal)
                .Replace("{FUTURE_DATE}", future.ToString("yyyy-MM-dd"), StringComparison.Ordinal);
        }

        return template.Sections.Select(sec => new RecordDataSection
        {
            Title = sec.Title,
            Fields = sec.Fields.Select(f => new RecordField
            {
                Label = ReplaceTokens(f.Label),
                Value = ReplaceTokens(f.Value)
            }).ToList()
        }).ToList();
    }
}
