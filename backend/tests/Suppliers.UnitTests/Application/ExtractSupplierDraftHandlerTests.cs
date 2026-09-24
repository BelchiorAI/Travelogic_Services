using FluentValidation;
using NSubstitute;
using Shouldly;
using Suppliers.Application.Abstractions;
using Suppliers.Application.Common;
using Suppliers.Application.Suppliers.Create;
using Suppliers.Application.Suppliers.Extract;
using Suppliers.Domain.Suppliers;

namespace Suppliers.UnitTests.Application;

public class ExtractSupplierDraftHandlerTests
{
    private const string RateSheet = "Marula Bush Lodge, Hazyview. Luxury suite R6800 per person per night.";

    private readonly ISupplierExtractionService _extraction = Substitute.For<ISupplierExtractionService>();
    private readonly ExtractSupplierDraftHandler _handler;

    public ExtractSupplierDraftHandlerTests()
    {
        _extraction.IsEnabled.Returns(true);
        _handler = new ExtractSupplierDraftHandler(_extraction, new ExtractSupplierDraftRequestValidator(), new CreateSupplierValidator());
    }

    private void ModelReturns(CreateSupplierRequest? draft) =>
        _extraction.ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(draft);

    [Fact]
    public async Task Valid_draft_is_returned_without_warnings()
    {
        ModelReturns(CreateSupplierValidatorTests.ValidRequest());

        var result = await _handler.HandleAsync(new ExtractSupplierDraftRequest(RateSheet), CancellationToken.None);

        result.Warnings.ShouldBeEmpty();
        result.Draft.Name.ShouldBe("Marula Bush Lodge");
        result.Draft.Services.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Invalid_or_missing_fields_become_warnings_with_form_keys()
    {
        ModelReturns(CreateSupplierValidatorTests.ValidRequest() with
        {
            Type = null,
            Services = [CreateSupplierValidatorTests.ValidService() with { Price = -5m, PricingUnit = null }],
        });

        var result = await _handler.HandleAsync(new ExtractSupplierDraftRequest(RateSheet), CancellationToken.None);

        result.Warnings.Select(w => w.Field).ShouldBe(
            ["Type", "Services[0].Price", "Services[0].PricingUnit"], ignoreOrder: true);
        result.Draft.Type.ShouldBeNull();
    }

    [Fact]
    public async Task Null_strings_from_the_model_are_normalised_and_currency_is_uppercased()
    {
        ModelReturns(new CreateSupplierRequest
        {
            Name = null!,
            City = null!,
            Country = null!,
            Services = [CreateSupplierValidatorTests.ValidService() with { Currency = " zar " }],
        });

        var result = await _handler.HandleAsync(new ExtractSupplierDraftRequest(RateSheet), CancellationToken.None);

        result.Draft.Name.ShouldBe("");
        result.Draft.Services[0].Currency.ShouldBe("ZAR");
        result.Warnings.Select(w => w.Field).ShouldBe(["Name", "Type", "City", "Country"], ignoreOrder: true);
    }

    [Fact]
    public async Task Unreadable_model_output_returns_an_empty_draft_with_a_general_warning()
    {
        ModelReturns(null);

        var result = await _handler.HandleAsync(new ExtractSupplierDraftRequest(RateSheet), CancellationToken.None);

        result.Draft.Name.ShouldBeEmpty();
        result.Warnings.ShouldHaveSingleItem().Field.ShouldBe("");
    }

    [Fact]
    public async Task Disabled_feature_throws_without_calling_the_model()
    {
        _extraction.IsEnabled.Returns(false);

        await Should.ThrowAsync<FeatureDisabledException>(() =>
            _handler.HandleAsync(new ExtractSupplierDraftRequest(RateSheet), CancellationToken.None));

        await _extraction.DidNotReceive().ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_text_is_rejected_without_calling_the_model(string text)
    {
        await Should.ThrowAsync<ValidationException>(() =>
            _handler.HandleAsync(new ExtractSupplierDraftRequest(text), CancellationToken.None));

        await _extraction.DidNotReceive().ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Text_over_20000_characters_is_rejected()
    {
        var text = new string('x', ExtractSupplierDraftRequest.MaxTextLength + 1);

        var ex = await Should.ThrowAsync<ValidationException>(() =>
            _handler.HandleAsync(new ExtractSupplierDraftRequest(text), CancellationToken.None));

        ex.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe("Text");
    }
}
