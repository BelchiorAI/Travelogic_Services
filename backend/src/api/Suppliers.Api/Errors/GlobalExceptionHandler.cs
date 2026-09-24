using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Suppliers.Application.Common;
using Suppliers.Domain.Common;

namespace Suppliers.Api.Errors;

/// <summary>Turns exceptions into RFC 7807 ProblemDetails. Unknown errors become a 500 with no internals leaked.</summary>
internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException validation => new HttpValidationProblemDetails(ToErrorDictionary(validation))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
            },
            DomainException domain => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The request breaks a business rule.",
                Detail = domain.Message,
            },
            ConflictException conflict => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = conflict.Message,
            },
            NotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not found",
                Detail = notFound.Message,
            },
            FeatureDisabledException disabled => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Feature not available",
                Detail = disabled.Message,
            },
            ExtractionFailedException failed => new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "The AI model failed",
                Detail = failed.Message,
            },
            BadHttpRequestException badRequest => new ProblemDetails
            {
                // Malformed JSON, unknown enum names, wrong types in the body or query string.
                Status = badRequest.StatusCode,
                Title = "The request could not be read.",
                Detail = badRequest.Message,
            },
            _ => null,
        };

        // Unknown errors are logged by the exception handler middleware; the client sees no details.
        problem ??= new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
        };

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static Dictionary<string, string[]> ToErrorDictionary(ValidationException exception) =>
        exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
}
