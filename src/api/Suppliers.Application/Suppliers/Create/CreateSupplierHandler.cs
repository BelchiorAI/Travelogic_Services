using FluentValidation;
using Suppliers.Application.Abstractions;
using Suppliers.Application.Common;
using Suppliers.Application.Suppliers.Common;
using Suppliers.Domain.Suppliers;

namespace Suppliers.Application.Suppliers.Create;

public sealed class CreateSupplierHandler(
    IValidator<CreateSupplierRequest> validator,
    ISupplierRepository repository,
    TimeProvider timeProvider)
{
    public async Task<SupplierDto> HandleAsync(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        if (await repository.ExistsAsync(request.Name, request.City, cancellationToken))
            throw new ConflictException($"A supplier named '{request.Name.Trim()}' already exists in {request.City.Trim()}.");

        var supplier = Supplier.Create(
            request.Name,
            request.Type!.Value,
            request.City,
            request.Country,
            timeProvider.GetUtcNow(),
            request.Description,
            request.ContactName,
            request.Email,
            request.Phone,
            request.Website,
            request.Address);

        foreach (var service in request.Services)
        {
            supplier.AddService(
                service.Name,
                service.Type!.Value,
                service.Price!.Value,
                service.Currency,
                service.PricingUnit!.Value,
                service.Description,
                service.DurationMinutes,
                service.Capacity);
        }

        // The aggregate and its services are inserted in a single SaveChanges, i.e. one transaction.
        await repository.AddAsync(supplier, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return supplier.ToDto();
    }
}
