namespace Suppliers.Application.Common;

/// <summary>The requested resource does not exist (becomes HTTP 404).</summary>
public sealed class NotFoundException(string message) : Exception(message);
