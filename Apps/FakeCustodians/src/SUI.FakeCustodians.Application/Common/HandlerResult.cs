using System.Diagnostics.CodeAnalysis;

namespace SUI.FakeCustodians.Application.Common
{
    public class HandlerResult<T>
        where T : class
    {
        private HandlerResult(T? result, FailureInfo? failure)
        {
            Failure = failure;
            Result = result;
        }

        public FailureInfo? Failure { get; init; }

        public T? Result { get; init; }

        [MemberNotNullWhen(true, nameof(Result))]
        [MemberNotNullWhen(false, nameof(Failure))]
        public bool IsSuccess => Failure == null;

        public static HandlerResult<T> Success(T result)
        {
            return new HandlerResult<T>(result, null);
        }

        public static HandlerResult<T> ValidationFailure(IReadOnlyCollection<ErrorInfo> errors)
        {
            return new HandlerResult<T>(null, new FailureInfo(FailureKind.Validation, errors));
        }

        public static HandlerResult<T> ValidationFailure(ErrorInfo error)
        {
            return new HandlerResult<T>(null, new FailureInfo(FailureKind.Validation, [error]));
        }

        public static HandlerResult<T> NotFound(IReadOnlyCollection<ErrorInfo> errors)
        {
            return new HandlerResult<T>(null, new FailureInfo(FailureKind.NotFound, errors));
        }

        public static HandlerResult<T> NotFound(string? errorMessage = null)
        {
            var errors = !string.IsNullOrWhiteSpace(errorMessage)
                ? new[] { new ErrorInfo(errorMessage) }
                : Array.Empty<ErrorInfo>();

            return new HandlerResult<T>(null, new FailureInfo(FailureKind.NotFound, errors));
        }

        public static HandlerResult<T> DataConcurrencyError(IReadOnlyCollection<ErrorInfo> errors)
        {
            return new HandlerResult<T>(null, new FailureInfo(FailureKind.DataConcurrency, errors));
        }

        public static HandlerResult<T> DataConcurrencyError(string errorMessage)
        {
            var errors = new[] { new ErrorInfo(errorMessage) };

            return new HandlerResult<T>(null, new FailureInfo(FailureKind.DataConcurrency, errors));
        }
    }

    public record FailureInfo
    {
        internal FailureInfo(FailureKind kind, IReadOnlyCollection<ErrorInfo> errors)
        {
            Kind = kind;
            Errors = errors;
        }

        public FailureKind Kind { get; init; }

        public IReadOnlyCollection<ErrorInfo> Errors { get; init; }
    }

    public record ErrorInfo
    {
        internal ErrorInfo(string scope, string message)
        {
            Scope = scope;
            Message = message;
        }

        internal ErrorInfo(string message)
        {
            Scope = null;
            Message = message;
        }

        public string? Scope { get; init; }

        public string Message { get; private set; }
    }

    public enum FailureKind
    {
        Unknown = 0,
        Validation = 400,
        DataConcurrency = 409,
        NotFound = 404,
    }
}
