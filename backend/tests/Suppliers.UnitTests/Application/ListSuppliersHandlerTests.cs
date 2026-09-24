using NSubstitute;
using Shouldly;
using Suppliers.Application.Abstractions;
using Suppliers.Application.Suppliers.Common;
using Suppliers.Application.Suppliers.List;
using Suppliers.Domain.Suppliers;

namespace Suppliers.UnitTests.Application;

public class ListSuppliersHandlerTests
{
    private readonly ISupplierQueries _queries = Substitute.For<ISupplierQueries>();

    private async Task<ListSuppliersQuery> PassedQuery(ListSuppliersQuery input)
    {
        ListSuppliersQuery? passed = null;
        _queries.ListAsync(Arg.Do<ListSuppliersQuery>(q => passed = q), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<SupplierSummaryDto>([], 1, 20, 0));

        await new ListSuppliersHandler(_queries).HandleAsync(input, CancellationToken.None);

        return passed.ShouldNotBeNull();
    }

    [Fact]
    public async Task Page_size_is_capped_at_50()
    {
        (await PassedQuery(new ListSuppliersQuery(PageSize: 500))).PageSize.ShouldBe(50);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Non_positive_page_size_falls_back_to_default(int pageSize)
    {
        (await PassedQuery(new ListSuppliersQuery(PageSize: pageSize))).PageSize.ShouldBe(ListSuppliersQuery.DefaultPageSize);
    }

    [Fact]
    public async Task Page_below_one_becomes_one()
    {
        (await PassedQuery(new ListSuppliersQuery(Page: -2))).Page.ShouldBe(1);
    }

    [Fact]
    public async Task Search_is_trimmed_and_blank_search_is_dropped()
    {
        (await PassedQuery(new ListSuppliersQuery(Search: "  lodge "))).Search.ShouldBe("lodge");
        (await PassedQuery(new ListSuppliersQuery(Search: "   "))).Search.ShouldBeNull();
    }

    [Fact]
    public async Task Type_filter_is_passed_through()
    {
        (await PassedQuery(new ListSuppliersQuery(Type: SupplierType.Transport))).Type.ShouldBe(SupplierType.Transport);
    }
}
