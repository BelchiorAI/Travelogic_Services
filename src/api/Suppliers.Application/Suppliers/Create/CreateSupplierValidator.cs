using FluentValidation;
using Suppliers.Domain.Suppliers;

namespace Suppliers.Application.Suppliers.Create;

public sealed class CreateSupplierValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(SupplierLimits.NameMaxLength);
        RuleFor(x => x.Type).NotNull().IsInEnum();
        RuleFor(x => x.City).NotEmpty().MaximumLength(SupplierLimits.CityMaxLength);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(SupplierLimits.CountryMaxLength);
        RuleFor(x => x.Description).MaximumLength(SupplierLimits.DescriptionMaxLength);
        RuleFor(x => x.ContactName).MaximumLength(SupplierLimits.ContactMaxLength);
        RuleFor(x => x.Phone).MaximumLength(SupplierLimits.PhoneMaxLength);
        RuleFor(x => x.Address).MaximumLength(SupplierLimits.AddressMaxLength);

        RuleFor(x => x.Email)
            .MaximumLength(SupplierLimits.ContactMaxLength)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Website)
            .MaximumLength(SupplierLimits.WebsiteMaxLength)
            .Must(BeAnHttpUrl).WithMessage("'{PropertyName}' must be a valid http or https URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));

        // Null only happens with an explicit "services": null; RuleForEach below needs a list.
        RuleFor(x => x.Services)
            .NotNull()
            .Must(s => s.Count <= SupplierLimits.MaxServicesPerSupplier)
            .WithMessage($"A supplier can have at most {SupplierLimits.MaxServicesPerSupplier} services.");

        // Produces keys like "Services[0].Price", which the frontend maps to form fields.
        RuleForEach(x => x.Services).SetValidator(new CreateServiceValidator());
    }

    private static bool BeAnHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public sealed class CreateServiceValidator : AbstractValidator<CreateServiceRequest>
{
    public CreateServiceValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(SupplierLimits.NameMaxLength);
        RuleFor(x => x.Type).NotNull().IsInEnum();
        RuleFor(x => x.Description).MaximumLength(SupplierLimits.DescriptionMaxLength);
        RuleFor(x => x.Price).NotNull().GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency)
            .NotEmpty()
            .Matches("^[A-Z]{3}$").WithMessage("'{PropertyName}' must be a 3-letter uppercase ISO code, e.g. ZAR.");
        RuleFor(x => x.PricingUnit).NotNull().IsInEnum();
        RuleFor(x => x.DurationMinutes).GreaterThan(0).When(x => x.DurationMinutes.HasValue);
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
    }
}
