namespace Northwind.Api.Models;

/// <summary>A customer record. Email is unique across the repository.</summary>
public sealed record Customer(
    Guid Id,
    string Name,
    string Email,
    string Country,
    string Tier,
    DateTimeOffset CreatedAt);

/// <summary>One line on an order. Totals are always computed, never stored.</summary>
public sealed record OrderLine(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice)
{
    public decimal LineTotal => decimal.Round(Quantity * UnitPrice, 2);
}

public enum OrderStatus
{
    Pending,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}

/// <summary>An order and its lifecycle state. Immutable: changes use <c>with</c>.</summary>
public sealed record Order(
    Guid Id,
    Guid CustomerId,
    IReadOnlyList<OrderLine> Lines,
    OrderStatus Status,
    DateTimeOffset PlacedAt)
{
    public decimal Total => decimal.Round(Lines.Sum(line => line.LineTotal), 2);
}
