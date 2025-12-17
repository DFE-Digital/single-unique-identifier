namespace SUI.SingleView.Application.Exceptions;

public class TransferException(string message, Exception? innerException = null)
    : Exception(message, innerException);
