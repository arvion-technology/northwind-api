using System.Text.Json;
using Northwind.Api.OpenApi;

namespace Northwind.Api.Tests;

public sealed class OpenApiTests
{
    [Fact]
    public void Document_is_valid_json_with_the_expected_version()
    {
        using var document = JsonDocument.Parse(OpenApiDocument.Json);
        var root = document.RootElement;
        Assert.Equal("3.0.3", root.GetProperty("openapi").GetString());
        Assert.Equal("Northwind API", root.GetProperty("info").GetProperty("title").GetString());
    }

    [Fact]
    public void Every_runtime_route_has_a_path_entry()
    {
        using var document = JsonDocument.Parse(OpenApiDocument.Json);
        var paths = document.RootElement.GetProperty("paths");
        var expected = new[]
        {
            "/health", "/api", "/openapi/v1.json",
            "/api/customers", "/api/customers/{id}", "/api/customers/{id}/orders",
            "/api/orders", "/api/orders/{id}", "/api/orders/{id}/cancel",
        };
        foreach (var path in expected)
            Assert.True(paths.TryGetProperty(path, out _), $"missing path {path}");
    }

    [Fact]
    public void Problem_details_schemas_are_documented()
    {
        using var document = JsonDocument.Parse(OpenApiDocument.Json);
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        foreach (var name in new[] { "Customer", "Order", "OrderPage", "Problem", "ValidationProblem" })
            Assert.True(schemas.TryGetProperty(name, out _), $"missing schema {name}");

        var validation = schemas.GetProperty("ValidationProblem");
        Assert.True(validation.GetProperty("properties").TryGetProperty("errors", out _));
    }

    [Fact]
    public void Orders_endpoint_accepts_the_documented_filters()
    {
        using var document = JsonDocument.Parse(OpenApiDocument.Json);
        var parameters = document.RootElement
            .GetProperty("paths").GetProperty("/api/orders").GetProperty("get")
            .GetProperty("parameters");
        var names = parameters.EnumerateArray().Select(item => item.GetProperty("name").GetString()).ToList();
        foreach (var name in new[] { "customerId", "status", "page", "pageSize", "sort", "order" })
            Assert.Contains(name, names);
    }
}
