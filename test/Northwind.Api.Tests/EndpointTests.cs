using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Northwind.Api.Contracts;
using Northwind.Api.Endpoints;
using Northwind.Api.Http;
using Northwind.Api.Models;

namespace Northwind.Api.Tests;

public sealed class EndpointTests
{
    [Fact]
    public void List_customers_returns_a_page()
    {
        var context = new TestContext();
        var result = CustomerEndpoints.List(context.CustomerService);
        var ok = Assert.IsType<Ok<PagedResult<CustomerResponse>>>(result);
        Assert.Equal(12, ok.Value!.Total);
        Assert.Equal(10, ok.Value.Items.Count);
    }

    [Fact]
    public void Invalid_paging_returns_a_problem_with_errors()
    {
        var context = new TestContext();
        var result = CustomerEndpoints.List(context.CustomerService, page: 0, pageSize: 500);
        var problem = Assert.IsType<ProblemHttpResult>(result);

        Assert.Equal(400, problem.StatusCode);
        Assert.Equal("Validation failed", problem.ProblemDetails.Title);
        Assert.NotNull(problem.ProblemDetails.Type);
        var errors = Assert.IsType<Dictionary<string, string[]>>(problem.ProblemDetails.Extensions["errors"]);
        Assert.Contains("page", errors.Keys);
        Assert.Contains("pageSize", errors.Keys);
    }

    [Fact]
    public void Missing_customer_returns_404_problem_details()
    {
        var context = new TestContext();
        var result = CustomerEndpoints.GetById(Guid.NewGuid(), context.CustomerService);
        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(404, problem.StatusCode);
        Assert.Contains("No customer", problem.ProblemDetails.Detail);
    }

    [Fact]
    public void Creating_a_customer_returns_201_with_location()
    {
        var context = new TestContext();
        var request = new CreateCustomerRequest("Sita Gurung", "sita@example.com", "Nepal", "growth");
        var result = CustomerEndpoints.Create(request, context.CustomerService);
        var created = Assert.IsType<Created<CustomerResponse>>(result);

        Assert.Equal("Sita Gurung", created.Value!.Name);
        Assert.Equal("growth", created.Value.Tier);
        Assert.Contains(created.Value.Id.ToString(), created.Location);
    }

    [Fact]
    public void Invalid_customer_body_returns_400_problem()
    {
        var context = new TestContext();
        var result = CustomerEndpoints.Create(new CreateCustomerRequest("", "bad", "", null), context.CustomerService);
        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(400, problem.StatusCode);
        Assert.True(((Dictionary<string, string[]>)problem.ProblemDetails.Extensions["errors"]!).Count >= 3);
    }

    [Fact]
    public void Duplicate_email_returns_409()
    {
        var context = new TestContext();
        var request = new CreateCustomerRequest("Copy Cat", "aarav.sharma@example.com", "Nepal", "free");
        var result = CustomerEndpoints.Create(request, context.CustomerService);
        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(409, problem.StatusCode);
        Assert.Contains("already exists", problem.ProblemDetails.Detail);
    }

    [Fact]
    public void Customer_orders_endpoint_is_scoped_to_the_customer()
    {
        var context = new TestContext();
        var result = CustomerEndpoints.ListOrders(TestContext.FirstCustomerId, context.CustomerService, context.OrderService);
        var ok = Assert.IsType<Ok<PagedResult<OrderResponse>>>(result);
        Assert.All(ok.Value!.Items, order => Assert.Equal(TestContext.FirstCustomerId, order.CustomerId));
    }

    [Fact]
    public void Orders_for_an_unknown_customer_return_404()
    {
        var context = new TestContext();
        var result = CustomerEndpoints.ListOrders(Guid.NewGuid(), context.CustomerService, context.OrderService);
        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public void Invalid_order_status_returns_a_field_error()
    {
        var context = new TestContext();
        var result = OrderEndpoints.List(context.OrderService, status: "refunded");
        var problem = Assert.IsType<ProblemHttpResult>(result);
        var errors = Assert.IsType<Dictionary<string, string[]>>(problem.ProblemDetails.Extensions["errors"]);
        Assert.Contains("status", errors.Keys);
    }

    [Fact]
    public void Creating_an_order_for_an_unknown_customer_returns_404()
    {
        var context = new TestContext();
        var request = new CreateOrderRequest(Guid.NewGuid(), new List<CreateOrderLine>
        {
            new(TestContext.ProductId, "Knife", 1, 10m),
        });
        var result = OrderEndpoints.Create(request, context.OrderService, context.CustomerService);
        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(404, problem.StatusCode);
    }

    [Fact]
    public void Creating_an_order_validates_lines_before_customer_lookup()
    {
        var context = new TestContext();
        var request = new CreateOrderRequest(TestContext.FirstCustomerId, new List<CreateOrderLine>
        {
            new(TestContext.ProductId, "Knife", 0, 10m),
        });
        var result = OrderEndpoints.Create(request, context.OrderService, context.CustomerService);
        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public void Cancelling_a_pending_order_returns_the_cancelled_order()
    {
        var context = new TestContext();
        var result = OrderEndpoints.Cancel(TestContext.PendingOrderId, context.OrderService);
        var ok = Assert.IsType<Ok<OrderResponse>>(result);
        Assert.Equal("cancelled", ok.Value!.Status);
    }

    [Fact]
    public void Cancelling_a_shipped_order_returns_409()
    {
        var context = new TestContext();
        var result = OrderEndpoints.Cancel(TestContext.ShippedOrderId, context.OrderService);
        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(409, problem.StatusCode);
        Assert.Contains("no longer be cancelled", problem.ProblemDetails.Detail);
    }

    [Fact]
    public void Cancel_unknown_order_returns_404()
    {
        var context = new TestContext();
        var result = OrderEndpoints.Cancel(Guid.NewGuid(), context.OrderService);
        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(404, problem.StatusCode);
    }
}
