using Northwind.Api.Contracts;
using Northwind.Api.Models;
using Northwind.Api.Repositories;
using Northwind.Api.Services;

namespace Northwind.Api.Tests;

public sealed class ServiceTests
{
    [Fact]
    public void Customer_list_is_paged_and_sorted_by_created_at()
    {
        var context = new TestContext();
        var page = context.CustomerService.List(null, 1, 10);

        Assert.Equal(12, page.Total);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(10, page.Items.Count);
        Assert.True(page.Items[0].CreatedAt <= page.Items[1].CreatedAt);
        Assert.Equal("Aarav Sharma", page.Items[0].Name);
    }

    [Fact]
    public void Second_customer_page_returns_the_remainder()
    {
        var context = new TestContext();
        var page = context.CustomerService.List(null, 2, 10);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.Page);
        Assert.Equal(12, page.Total);
    }

    [Fact]
    public void Search_matches_name_email_and_country()
    {
        var context = new TestContext();
        Assert.Equal(2, context.CustomerService.List("Nepal", 1, 10).Total);
        Assert.Equal(1, context.CustomerService.List("kenji.watanabe", 1, 10).Total);
        Assert.Equal(0, context.CustomerService.List("Atlantis", 1, 10).Total);
    }

    [Fact]
    public void Customer_sort_by_name_descending_is_stable()
    {
        var context = new TestContext();
        var page = context.CustomerService.List(null, 1, 12, sort: "name", order: "desc");
        var names = page.Items.Select(customer => customer.Name).ToList();
        Assert.Equal(names.OrderByDescending(name => name, StringComparer.OrdinalIgnoreCase), names);
    }

    [Fact]
    public void Order_list_defaults_to_newest_first()
    {
        var context = new TestContext();
        var page = context.OrderService.List(null, null, 1, 10);
        Assert.Equal(24, page.Total);
        Assert.True(page.Items[0].PlacedAt >= page.Items[1].PlacedAt);
    }

    [Fact]
    public void Orders_can_be_filtered_by_status_and_customer()
    {
        var context = new TestContext();
        var pending = context.OrderService.List(null, "pending", 1, 50);
        Assert.Equal(3, pending.Total);
        Assert.All(pending.Items, order => Assert.Equal("pending", order.Status));

        var customerOrders = context.OrderService.List(TestContext.FirstCustomerId, null, 1, 50);
        Assert.All(customerOrders.Items, order => Assert.Equal(TestContext.FirstCustomerId, order.CustomerId));
        Assert.True(customerOrders.Total >= 2);
    }

    [Fact]
    public void Creating_an_order_computes_line_totals_and_order_total()
    {
        var context = new TestContext();
        var created = context.OrderService.Create(TestContext.FirstCustomerId, new List<CreateOrderLine>
        {
            new(TestContext.ProductId, "Khukuri chef knife", 2, 89.00m),
            new(TestContext.SecondProductId, "Pashmina throw", 1, 64.50m),
        });

        Assert.Equal("pending", created.Status);
        Assert.Equal(178.00m, created.Lines[0].LineTotal);
        Assert.Equal(242.50m, created.Total);
        Assert.Equal(25, context.OrderService.List(null, null, 1, 100).Total);
    }

    [Fact]
    public void Pending_orders_cancel_and_others_conflict()
    {
        var context = new TestContext();
        var cancelled = context.OrderService.Cancel(TestContext.PendingOrderId);
        Assert.Equal(CancelOutcome.Cancelled, cancelled.Outcome);
        Assert.Equal("cancelled", cancelled.Order!.Status);

        var shipped = context.OrderService.Cancel(TestContext.ShippedOrderId);
        Assert.Equal(CancelOutcome.NotCancellable, shipped.Outcome);
        Assert.Equal("shipped", shipped.Order!.Status);
    }

    [Fact]
    public void Cancelling_an_unknown_order_is_not_found()
    {
        var context = new TestContext();
        var result = context.OrderService.Cancel(Guid.NewGuid());
        Assert.Equal(CancelOutcome.NotFound, result.Outcome);
        Assert.Null(result.Order);
    }

    [Fact]
    public void Unknown_ids_return_null_from_the_services()
    {
        var context = new TestContext();
        Assert.Null(context.CustomerService.Get(Guid.NewGuid()));
        Assert.Null(context.OrderService.Get(Guid.NewGuid()));
    }

    [Fact]
    public void Creating_a_customer_normalises_email_and_defaults_tier()
    {
        var context = new TestContext();
        var created = context.CustomerService.Create("New Person", "NEW@Example.COM", " Nepal ", "GROWTH");
        Assert.Equal("new@example.com", created.Email);
        Assert.Equal("Nepal", created.Country);
        Assert.Equal("growth", created.Tier);
        Assert.True(context.CustomerService.EmailExists("new@example.com"));
    }
}
