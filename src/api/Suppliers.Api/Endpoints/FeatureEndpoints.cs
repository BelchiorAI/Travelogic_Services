using Microsoft.AspNetCore.Http.HttpResults;

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

    // AI extraction arrives in Phase 7; until then it is always off.
    private static Ok<FeaturesResponse> GetFeatures() => TypedResults.Ok(new FeaturesResponse(AiExtraction: false));
}
