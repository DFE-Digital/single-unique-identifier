namespace SUI.Find.Application.Enums;

public enum JobType
{
    Unknown,

    /// <summary>
    /// Custodian Lookup jobs indicate the Custodian needs to look for the records they hold about a specific SUI,
    /// and then submit pointers to those records back to the SUI System.
    /// Custodian Lookup is essentially the job in response to a Search Request.
    /// </summary>
    CustodianLookup,
}
