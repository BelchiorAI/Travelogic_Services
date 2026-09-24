using FluentValidation;
using NSubstitute;
using Shouldly;
using Suppliers.Application.Abstractions;
using Suppliers.Application.Common;
using Suppliers.Application.Suppliers.Create;
using Suppliers.Domain.Suppliers;

namespace Suppliers.UnitTests.Application;

public class CreateSupplierHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 8, 0, 0, TimeSpan.Zero);

    private readonly ISupplierRepository _repository = Substitute.For<ISupplierRepository>();
    private readonly CreateSupplierHandler _handler;

    public CreateSupplierHandlerTests()
    {
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(Now);
        _handler = new CreateSupplierHandler(new CreateSupplierValidator(), _repository, timeProvider);
    }

    [Fact]
    public async Task Valid_request_saves_the_supplier_and_returns_it()
    {
        var request = CreateSupplierValidatorTests.ValidRequest();

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        result.Name.ShouldBe("Marula Bush Lodge");
        result.CreatedAt.ShouldBe(Now);
        result.Services.ShouldHaveSingleItem().Name.ShouldBe("Sunrise Game Drive");

        await _repository.Received(1).AddAsync(
            Arg.Is<Supplier>(s => s.Id == result.Id && s.Services.Count == 1), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicate_name_and_city_throws_conflict_and_saves_nothing()
    {
        _repository.ExistsAsync("Marula Bush Lodge", "Hazyview", Arg.Any<CancellationToken>()).Returns(true);

        await Should.ThrowAsync<ConflictException>(() =>
            _handler.HandleAsync(CreateSupplierValidatorTests.ValidRequest(), CancellationToken.None));

        await _repository.DidNotReceive().AddAsync(Arg.Any<Supplier>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invalid_request_throws_validation_exception_before_touching_the_repository()
    {
        var request = CreateSupplierValidatorTests.ValidRequest() with { Name = "" };

        var ex = await Should.ThrowAsync<ValidationException>(() => _handler.HandleAsync(request, CancellationToken.None));

        ex.Errors.ShouldContain(e => e.PropertyName == "Name");
        _repository.ReceivedCalls().ShouldBeEmpty();
    }
}
