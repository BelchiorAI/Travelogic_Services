namespace Suppliers.Application.Common;

/// <summary>The request clashes with existing data (becomes HTTP 409).</summary>
public sealed class ConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
