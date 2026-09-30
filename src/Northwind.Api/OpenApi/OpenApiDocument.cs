namespace Northwind.Api.OpenApi;

using System.Reflection;

/// <summary>
/// The OpenAPI document is written by hand and shipped as an embedded resource.
/// That is deliberate: this sample does not need a NuGet package to describe
/// eight routes, and keeping it in version control makes the contract diffable.
/// </summary>
public static class OpenApiDocument
{
    private static readonly Lazy<string> Document = new(Load);

    public static string Json => Document.Value;

    private static string Load()
    {
        var assembly = typeof(OpenApiDocument).Assembly;
        using var stream = assembly.GetManifestResourceStream("Northwind.Api.openapi.json")
            ?? throw new InvalidOperationException("openapi.json was not embedded in the assembly.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
