using Microsoft.AspNetCore.Http.HttpResults;
using Suppliers.Application.Suppliers.Common;
using Suppliers.Application.Suppliers.Create;
using Suppliers.Application.Suppliers.GetById;
using Suppliers.Application.Suppliers.List;
using Suppliers.Domain.Suppliers;

namespace Suppliers.Api.Endpoints;

internal static class SupplierEndpoints
{
    public static RouteGroupBuilder MapSupplierEndpoints(this RouteGroupBuilder v1)
    {
        var suppliers = v1.MapGroup("/suppliers").WithTags("Suppliers");

        suppliers.MapGet("/", ListSuppliers)
            .WithName("ListSuppliers")
            .WithSummary("List suppliers")
            .WithDescription("Paged list, optionally filtered by a name search and a supplier type. Page size is capped at 50.")
            .ProducesValidationProblem();

        suppliers.MapGet("/{id:guid}", GetSupplierById)
            .WithName("GetSupplierById")
            .WithSummary("Get a supplier with its services");

        suppliers.MapPost("/", CreateSupplier)
            .WithName("CreateSupplier")
            .WithSummary("Create a supplier with its services")
            .WithDescription("Validation errors use keys like 'Services[0].Price'. A supplier with the same name in the same city returns 409.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        return v1;
    }

    /// <summary>Query-string parameters for the list endpoint.</summary>
    internal sealed record ListSuppliersParameters(int? Page, int? PageSize, string? Search, SupplierType? Type);

    private static async Task<Ok<PagedResult<SupplierSummaryDto>>> ListSuppliers(
        [AsParameters] ListSuppliersParameters parameters,
        ListSuppliersHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new ListSuppliersQuery(
            parameters.Page ?? 1,
            parameters.PageSize ?? ListSuppliersQuery.DefaultPageSize,
            parameters.Search,
            parameters.Type);

        return TypedResults.Ok(await handler.HandleAsync(query, cancellationToken));
    }

    private static async Task<Results<Ok<SupplierDto>, NotFound>> GetSupplierById(
        Guid id,
        GetSupplierByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var supplier = await handler.HandleAsync(id, cancellationToken);
        return supplier is null ? TypedResults.NotFound() : TypedResults.Ok(supplier);
    }

    private static async Task<Created<SupplierDto>> CreateSupplier(
        CreateSupplierRequest request,
        CreateSupplierHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var supplier = await handler.HandleAsync(request, cancellationToken);
        var location = $"{httpContext.Request.PathBase}{httpContext.Request.Path.Value?.TrimEnd('/')}/{supplier.Id}";
        return TypedResults.Created(location, supplier);
    }
}
