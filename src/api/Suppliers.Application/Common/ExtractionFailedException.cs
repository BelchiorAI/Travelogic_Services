namespace Suppliers.Application.Common;

/// <summary>The AI model could not be reached or did not answer in time (becomes HTTP 502).</summary>
public sealed class ExtractionFailedException(string message, Exception? innerException = null)
    : Exception(message, innerException);
