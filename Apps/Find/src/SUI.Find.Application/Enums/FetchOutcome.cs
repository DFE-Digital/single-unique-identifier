namespace SUI.Find.Application.Enums;

public enum FetchOutcome
{
    Success,
    AuthorizationFailure, // The requester is not authorized to access this search job
    PolicyDenial, // The policy enforcement point denied access to the record
    NetworkError,
    JobNotFound, // The specified search job does not exist
    RecordNotFound, // The url requested returned 404
}
