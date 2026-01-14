namespace SUI.Find.Application.Enums;

public enum FetchOutcome
{
    Success,

    /// <summary>
    /// The requester is not authorized to access this search job
    /// </summary>
    AuthorizationFailure,

    /// <summary>
    /// The policy enforcement point denied access to the record
    /// </summary>
    PolicyDenial,

    NetworkError,

    /// <summary>
    /// The specified search job does not exist
    /// </summary>
    JobNotFound,

    /// <summary>
    /// The url requested returned 404
    /// </summary>
    RecordNotFound,
}
