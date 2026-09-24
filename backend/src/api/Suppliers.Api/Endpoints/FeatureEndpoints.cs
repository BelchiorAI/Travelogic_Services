using Microsoft.AspNetCore.Http.HttpResults;
using Suppliers.Application.Abstractions;

namespace Suppliers.Api.Endpoints;

internal static class FeatureEndpoints
{
    /// <summary>Optional features the frontend can switch on.</summary>
    internal sealed record FeaturesResponse(bool AiExtraction);

    public static RouteGroupBuilder MapFeatureEndpoints(this RouteGroupBuilder v1)
    {
        v1.MapGet("/features", GetFeatures)
            .WithName("GetFeatures")
            .WithTags("Features")
            .WithSummary("Which optional features are enabled");

        return v1;
    }

    private static Ok<FeaturesResponse> GetFeatures(ISupplierExtractionService extraction) =>
        TypedResults.Ok(new FeaturesResponse(AiExtraction: extraction.IsEnabled));
}
