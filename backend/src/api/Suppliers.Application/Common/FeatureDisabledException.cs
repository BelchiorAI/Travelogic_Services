namespace Suppliers.Application.Common;

/// <summary>An optional feature is switched off on this server (becomes HTTP 503).</summary>
public sealed class FeatureDisabledException(string message) : Exception(message);
