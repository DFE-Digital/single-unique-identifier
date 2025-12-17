namespace SUI.SingleView.Application.Exceptions;

public class RecordException(string message, Exception? innerException = null)
    : Exception(message, innerException);
