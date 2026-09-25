namespace Northwind.Api.Endpoints;

using Northwind.Api.Contracts;
using Northwind.Api.OpenApi;

public static class MetaEndpoints
{
    public static IEndpointRouteBuilder MapMetaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => TypedResults.Ok(new
        {
            status = "ok",
            service = "northwind-api",
            version = "1.0.0",
        })).WithTags("Meta");

        app.MapGet("/api", () => TypedResults.Ok(new ApiInfo(
            "Northwind API",
            "1.0.0",
            "/openapi/v1.json",
            new[]
            {
                "GET /api/customers",
                "GET /api/customers/{id}",
                "POST /api/customers",
                "GET /api/customers/{id}/orders",
                "GET /api/orders",
                "GET /api/orders/{id}",
                "POST /api/orders",
                "POST /api/orders/{id}/cancel",
            }
        ))).WithTags("Meta");

        app.MapGet("/openapi/v1.json", () => TypedResults.Text(OpenApiDocument.Json, "application/json"))
            .WithTags("Meta");

        return app;
    }
}
