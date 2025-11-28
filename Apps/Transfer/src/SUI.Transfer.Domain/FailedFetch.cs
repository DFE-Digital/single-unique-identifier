namespace SUI.Transfer.Domain;

public record FailedFetch(RecordPointer RecordPointer, string ErrorMessage);
