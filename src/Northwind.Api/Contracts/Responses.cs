namespace Northwind.Api.Contracts;

using Northwind.Api.Models;

/// <summary>What a customer looks like on the wire.</summary>
public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string Email,
    string Country,
    string Tier,
    DateTimeOffset CreatedAt);

public sealed record CreateCustomerRequest(
    string? Name,
    string? Email,
    string? Country,
    string? Tier);

public sealed record CreateOrderLine(
    Guid ProductId,
    string? ProductName,
    int Quantity,
    decimal UnitPrice);

public sealed record CreateOrderRequest(
    Guid CustomerId,
    IReadOnlyList<CreateOrderLine>? Lines);

public sealed record OrderLineResponse(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    IReadOnlyList<OrderLineResponse> Lines,
    string Status,
    decimal Total,
    DateTimeOffset PlacedAt);

/// <summary>One page of a collection. TotalPages is computed, not supplied.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages);

public sealed record ApiInfo(
    string Name,
    string Version,
    string Documentation,
    IReadOnlyList<string> Endpoints);

public static class Mappings
{
    public static CustomerResponse ToResponse(this Customer customer) => new(
        customer.Id,
        customer.Name,
        customer.Email,
        customer.Country,
        customer.Tier,
        customer.CreatedAt);

    public static OrderResponse ToResponse(this Order order) => new(
        order.Id,
        order.CustomerId,
        order.Lines.Select(line => new OrderLineResponse(
            line.ProductId,
            line.ProductName,
            line.Quantity,
            line.UnitPrice,
            line.LineTotal)).ToList(),
        order.Status.ToString().ToLowerInvariant(),
        order.Total,
        order.PlacedAt);
}
