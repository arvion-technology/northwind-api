namespace Northwind.Api.Validation;

using System.Text.RegularExpressions;
using Northwind.Api.Contracts;
using Northwind.Api.Models;

/// <summary>
/// Validation returns a field -> messages map. Endpoints turn that into a
/// problem-details response; nothing here knows about HTTP.
/// </summary>
public static class RequestValidator
{
    private static readonly Regex EmailPattern = new(
        @"^[^@\s]+@[^@\s]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static readonly string[] Tiers = { "free", "growth", "enterprise" };
    public static readonly string[] CustomerSorts = { "name", "country", "tier", "createdAt" };
    public static readonly string[] OrderSorts = { "placedAt", "total", "status" };
    public static readonly string[] Directions = { "asc", "desc" };

    public const int MaxPageSize = 100;
    public const int MaxOrderLines = 20;
    public const int MaxQuantity = 25;
    public const decimal MaxUnitPrice = 100_000m;

    public static Dictionary<string, string[]> ValidateCreateCustomer(CreateCustomerRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 2)
            errors["name"] = new[] { "Name must be at least 2 characters." };
        else if (request.Name.Trim().Length > 80)
            errors["name"] = new[] { "Name must be 80 characters or fewer." };

        if (string.IsNullOrWhiteSpace(request.Email) || !EmailPattern.IsMatch(request.Email.Trim()))
            errors["email"] = new[] { "Email must be a valid address." };

        if (string.IsNullOrWhiteSpace(request.Country) || request.Country.Trim().Length is < 2 or > 56)
            errors["country"] = new[] { "Country must be 2 to 56 characters." };

        if (!string.IsNullOrWhiteSpace(request.Tier) && !Tiers.Contains(request.Tier.Trim().ToLowerInvariant()))
            errors["tier"] = new[] { "Tier must be one of: free, growth, enterprise." };

        return errors;
    }

    public static Dictionary<string, string[]> ValidateCreateOrder(CreateOrderRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request.CustomerId == Guid.Empty)
            errors["customerId"] = new[] { "CustomerId is required." };

        if (request.Lines is null || request.Lines.Count == 0)
            errors["lines"] = new[] { "An order needs at least one line." };
        else if (request.Lines.Count > MaxOrderLines)
            errors["lines"] = new[] { $"An order can have at most {MaxOrderLines} lines." };
        else
        {
            for (var index = 0; index < request.Lines.Count; index++)
            {
                var line = request.Lines[index];
                var prefix = $"lines[{index}]";
                if (line.ProductId == Guid.Empty)
                    errors[$"{prefix}.productId"] = new[] { "ProductId is required." };
                if (string.IsNullOrWhiteSpace(line.ProductName) || line.ProductName.Trim().Length > 60)
                    errors[$"{prefix}.productName"] = new[] { "ProductName is required, 60 characters or fewer." };
                if (line.Quantity is < 1 or > MaxQuantity)
                    errors[$"{prefix}.quantity"] = new[] { $"Quantity must be between 1 and {MaxQuantity}." };
                if (line.UnitPrice <= 0 || line.UnitPrice > MaxUnitPrice)
                    errors[$"{prefix}.unitPrice"] = new[] { $"UnitPrice must be above 0 and at most {MaxUnitPrice}." };
            }

            var duplicates = request.Lines
                .GroupBy(line => line.ProductId)
                .Where(group => group.Count() > 1 && group.Key != Guid.Empty)
                .Select(group => group.Key)
                .ToList();
            if (duplicates.Count > 0)
                errors["lines"] = new[] { "Each product may appear only once; merge duplicate lines." };
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidatePaging(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (page < 1) errors["page"] = new[] { "Page must be 1 or greater." };
        if (pageSize is < 1 or > MaxPageSize)
            errors["pageSize"] = new[] { $"PageSize must be between 1 and {MaxPageSize}." };
        return errors;
    }

    public static Dictionary<string, string[]> ValidateQuery(string? sort, string? order, string[] allowedSorts)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(sort) && !allowedSorts.Contains(sort.Trim(), StringComparer.OrdinalIgnoreCase))
            errors["sort"] = new[] { $"Sort must be one of: {string.Join(", ", allowedSorts)}." };
        if (!string.IsNullOrWhiteSpace(order) && !Directions.Contains(order.Trim(), StringComparer.OrdinalIgnoreCase))
            errors["order"] = new[] { "Order must be asc or desc." };
        return errors;
    }

    public static bool TryParseStatus(string? status, out OrderStatus parsed)
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(status)) return true;
        return Enum.TryParse(status.Trim(), ignoreCase: true, out parsed) && Enum.IsDefined(parsed);
    }

    public static Dictionary<string, string[]> Merge(params Dictionary<string, string[]>[] maps)
    {
        var merged = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var map in maps)
        {
            foreach (var (field, messages) in map)
                merged[field] = messages;
        }
        return merged;
    }
}
