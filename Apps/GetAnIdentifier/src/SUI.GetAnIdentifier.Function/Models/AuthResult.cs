using System.Diagnostics.CodeAnalysis;

namespace SUI.GetAnIdentifier.Function.Models;

public class AuthResult
{
    [MemberNotNullWhen(true, nameof(Context))]
    [MemberNotNullWhen(false, nameof(FailureType), nameof(ErrorMessage))]
    public bool IsSuccess { get; }
    public AuthContext? Context { get; }
    public AuthFailureReason? FailureType { get; }
    public string? ErrorMessage { get; }

    private AuthResult(
        bool isSuccess,
        AuthContext? context,
        AuthFailureReason? failureType,
        string? errorMessage
    )
    {
        IsSuccess = isSuccess;
        Context = context;
        FailureType = failureType;
        ErrorMessage = errorMessage;
    }

    public static AuthResult Success(AuthContext context) => new(true, context, null, null);

    public static AuthResult Failure(AuthFailureReason reason, string errorMessage) =>
        new(false, null, reason, errorMessage);
}

public enum AuthFailureReason
{
    InvalidTokenClaims,
    ClientNotFound,
    ClientDisabled,
    MissingOrganisationId,
}
