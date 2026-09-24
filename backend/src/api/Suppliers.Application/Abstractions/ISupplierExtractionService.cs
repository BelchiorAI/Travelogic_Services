using Suppliers.Application.Suppliers.Create;

namespace Suppliers.Application.Abstractions;

/// <summary>Port for turning free text (a rate sheet, a contract) into a supplier draft. Implemented in Infrastructure.</summary>
public interface ISupplierExtractionService
{
    /// <summary>False when no AI model is configured; the app then runs without extraction.</summary>
    bool IsEnabled { get; }

    /// <returns>The draft, or <c>null</c> when the model's output could not be read.</returns>
    /// <exception cref="Common.ExtractionFailedException">The model could not be reached or did not answer in time.</exception>
    Task<CreateSupplierRequest?> ExtractAsync(string text, CancellationToken cancellationToken);
}
