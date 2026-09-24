using FluentValidation;
using Suppliers.Application.Abstractions;
using Suppliers.Application.Common;
using Suppliers.Application.Suppliers.Create;

namespace Suppliers.Application.Suppliers.Extract;

public sealed record ExtractSupplierDraftRequest(string Text)
{
    public const int MaxTextLength = 20_000;
}

/// <param name="Field">Same key format as validation errors, e.g. "Services[0].Price", so the form can highlight it.</param>
public sealed record ExtractionWarning(string Field, string Message);

public sealed record ExtractSupplierDraftResult(CreateSupplierRequest Draft, IReadOnlyList<ExtractionWarning> Warnings);

public sealed class ExtractSupplierDraftRequestValidator : AbstractValidator<ExtractSupplierDraftRequest>
{
    public ExtractSupplierDraftRequestValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(ExtractSupplierDraftRequest.MaxTextLength);
    }
}

/// <summary>
/// Turns pasted text into a draft that pre-fills the create form. It never saves anything:
/// the user reviews the draft, fixes the warnings and submits it through the normal create flow.
/// </summary>
public sealed class ExtractSupplierDraftHandler(
    ISupplierExtractionService extractionService,
    IValidator<ExtractSupplierDraftRequest> requestValidator,
    IValidator<CreateSupplierRequest> draftValidator)
{
    public async Task<ExtractSupplierDraftResult> HandleAsync(ExtractSupplierDraftRequest request, CancellationToken cancellationToken)
    {
        if (!extractionService.IsEnabled)
            throw new FeatureDisabledException("AI extraction is not enabled on this server.");

        await requestValidator.ValidateAndThrowAsync(request, cancellationToken);

        var extracted = await extractionService.ExtractAsync(request.Text, cancellationToken);
        if (extracted is null)
        {
            return new ExtractSupplierDraftResult(
                new CreateSupplierRequest(),
                [new ExtractionWarning("", "The AI could not read a supplier from this text. Please fill in the form manually.")]);
        }

        var draft = Normalise(extracted);
        var validation = await draftValidator.ValidateAsync(draft, cancellationToken);
        var warnings = validation.Errors
            .Select(e => new ExtractionWarning(e.PropertyName, e.ErrorMessage))
            .ToList();

        return new ExtractSupplierDraftResult(draft, warnings);
    }

    // Model output can contain explicit nulls where our contract has empty strings or lists.
    private static CreateSupplierRequest Normalise(CreateSupplierRequest draft) => draft with
    {
        Name = draft.Name?.Trim() ?? string.Empty,
        City = draft.City?.Trim() ?? string.Empty,
        Country = draft.Country?.Trim() ?? string.Empty,
        Services = (draft.Services ?? [])
            .Where(s => s is not null)
            .Select(s => s with
            {
                Name = s.Name?.Trim() ?? string.Empty,
                Currency = s.Currency?.Trim().ToUpperInvariant() ?? string.Empty,
            })
            .ToList(),
    };
}
