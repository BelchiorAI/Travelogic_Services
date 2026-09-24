using Suppliers.Domain.Suppliers;

namespace Suppliers.Application.Suppliers.List;

public sealed record ListSuppliersQuery(int Page = 1, int PageSize = ListSuppliersQuery.DefaultPageSize, string? Search = null, SupplierType? Type = null)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;

    /// <summary>Clamps paging to sane bounds and trims the search term.</summary>
    public ListSuppliersQuery Normalise() => this with
    {
        Page = Math.Max(1, Page),
        PageSize = PageSize < 1 ? DefaultPageSize : Math.Min(PageSize, MaxPageSize),
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
    };
}
