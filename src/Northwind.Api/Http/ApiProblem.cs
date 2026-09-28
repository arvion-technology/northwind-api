namespace Northwind.Api.Http;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// RFC 7807 responses. Every error leaves the API through one of these helpers so
/// the shape (type, title, status, detail, errors) is the same everywhere.
/// </summary>
public static class ApiProblem
{
    private const string BaseType = "https://httpstatuses.io";

    public static ProblemHttpResult Validation(IDictionary<string, string[]> errors)
    {
        var details = new ProblemDetails
        {
            Type = $"{BaseType}/400",
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = "The request body or query string failed validation.",
        };
        details.Extensions["errors"] = errors;
        return TypedResults.Problem(details);
    }

    public static ProblemHttpResult NotFound(string detail) =>
        TypedResults.Problem(
            detail: detail,
            title: "Resource not found",
            type: $"{BaseType}/404",
            statusCode: StatusCodes.Status404NotFound);

    public static ProblemHttpResult Conflict(string detail) =>
        TypedResults.Problem(
            detail: detail,
            title: "Request conflicts with the current state",
            type: $"{BaseType}/409",
            statusCode: StatusCodes.Status409Conflict);
}
