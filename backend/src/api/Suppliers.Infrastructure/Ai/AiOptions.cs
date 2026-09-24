using System.Diagnostics.CodeAnalysis;

namespace Suppliers.Infrastructure.Ai;

/// <summary>Bound from the "Ai" configuration section. Extraction is on only when enabled and fully configured.</summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public bool Enabled { get; init; }

    /// <summary>Model name, e.g. from OpenAI or Gemini's OpenAI-compatible API.</summary>
    public string? Model { get; init; }

    public string? ApiKey { get; init; }

    /// <summary>Optional OpenAI-compatible endpoint; leave empty for OpenAI itself.</summary>
    public string? Endpoint { get; init; }

    public int TimeoutSeconds { get; init; } = 30;

    [MemberNotNullWhen(true, nameof(Model), nameof(ApiKey))]
    public bool IsConfigured =>
        Enabled && !string.IsNullOrWhiteSpace(Model) && !string.IsNullOrWhiteSpace(ApiKey);
}
