using Northwind.Api.Contracts;
using Northwind.Api.Validation;

namespace Northwind.Api.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void Valid_customer_request_passes()
    {
        var request = new CreateCustomerRequest("Sita Gurung", "sita@example.com", "Nepal", "growth");
        Assert.Empty(RequestValidator.ValidateCreateCustomer(request));
    }

    [Fact]
    public void Invalid_customer_fields_are_reported_by_name()
    {
        var request = new CreateCustomerRequest(" ", "not-an-email", "N", "platinum");
        var errors = RequestValidator.ValidateCreateCustomer(request);

        Assert.Equal(new[] { "country", "email", "name", "tier" }, errors.Keys.OrderBy(key => key).ToArray());
        Assert.Contains("Email", errors["email"][0]);
        Assert.Contains("2 to 56", errors["country"][0]);
    }

    [Fact]
    public void Paging_boundaries_are_enforced()
    {
        Assert.Empty(RequestValidator.ValidatePaging(1, 10));
        Assert.Empty(RequestValidator.ValidatePaging(1, 100));
        Assert.Contains("page", RequestValidator.ValidatePaging(0, 10).Keys);
        Assert.Contains("pageSize", RequestValidator.ValidatePaging(1, 101).Keys);
        Assert.Contains("pageSize", RequestValidator.ValidatePaging(1, 0).Keys);
    }

    [Fact]
    public void Query_sort_and_direction_are_validated()
    {
        Assert.Empty(RequestValidator.ValidateQuery("name", "desc", RequestValidator.CustomerSorts));
        Assert.Empty(RequestValidator.ValidateQuery(null, null, RequestValidator.CustomerSorts));
        Assert.Contains("sort", RequestValidator.ValidateQuery("password", "asc", RequestValidator.CustomerSorts).Keys);
        Assert.Contains("order", RequestValidator.ValidateQuery("name", "sideways", RequestValidator.CustomerSorts).Keys);
    }

    [Fact]
    public void Valid_order_request_passes()
    {
        var request = new CreateOrderRequest(TestContext.FirstCustomerId, new List<CreateOrderLine>
        {
            new(TestContext.ProductId, "Khukuri chef knife", 2, 89.00m),
        });
        Assert.Empty(RequestValidator.ValidateCreateOrder(request));
    }

    [Fact]
    public void Order_requires_a_customer_and_at_least_one_line()
    {
        var noLines = new CreateOrderRequest(Guid.Empty, new List<CreateOrderLine>());
        var errors = RequestValidator.ValidateCreateOrder(noLines);

        Assert.Contains("customerId", errors.Keys);
        Assert.Contains("lines", errors.Keys);
    }

    [Fact]
    public void Order_line_bounds_and_duplicates_are_reported_with_indexes()
    {
        var request = new CreateOrderRequest(TestContext.FirstCustomerId, new List<CreateOrderLine>
        {
            new(TestContext.ProductId, "", 0, -1m),
            new(TestContext.ProductId, "Duplicate product", 1, 10m),
        });
        var errors = RequestValidator.ValidateCreateOrder(request);

        Assert.Contains("lines[0].productName", errors.Keys);
        Assert.Contains("lines[0].quantity", errors.Keys);
        Assert.Contains("lines[0].unitPrice", errors.Keys);
        Assert.Contains("lines", errors.Keys);
    }

    [Fact]
    public void Order_status_parsing_is_case_insensitive_and_rejects_unknown_values()
    {
        Assert.True(RequestValidator.TryParseStatus("Paid", out var paid));
        Assert.Equal(Northwind.Api.Models.OrderStatus.Paid, paid);
        Assert.True(RequestValidator.TryParseStatus("  delivered ", out var delivered));
        Assert.Equal(Northwind.Api.Models.OrderStatus.Delivered, delivered);
        Assert.True(RequestValidator.TryParseStatus(null, out _));
        Assert.False(RequestValidator.TryParseStatus("refunded", out _));
        Assert.False(RequestValidator.TryParseStatus("9", out _));
    }

    [Fact]
    public void Merge_keeps_every_map()
    {
        var merged = RequestValidator.Merge(
            RequestValidator.ValidatePaging(0, 200),
            RequestValidator.ValidateQuery("wrong", "up", RequestValidator.OrderSorts));
        Assert.Equal(4, merged.Count);
    }
}
