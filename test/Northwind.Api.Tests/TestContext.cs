using System.Text.Json;
using Northwind.Api.Contracts;
using Northwind.Api.Http;
using Northwind.Api.Models;
using Northwind.Api.OpenApi;
using Northwind.Api.Repositories;
using Northwind.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Northwind.Api.Tests;

/// <summary>
/// Shared fixture: fresh in-memory repositories with the deterministic seed data.
/// Handler tests call the endpoint methods directly, so no web host is needed.
/// </summary>
public sealed class TestContext
{
    public ICustomerRepository Customers { get; }
    public IOrderRepository Orders { get; }
    public CustomerService CustomerService { get; }
    public OrderService OrderService { get; }

    public TestContext()
    {
        Customers = new InMemoryCustomerRepository(SeedData.Customers());
        Orders = new InMemoryOrderRepository(SeedData.Orders());
        CustomerService = new CustomerService(Customers);
        OrderService = new OrderService(Orders);
    }

    public static readonly Guid FirstCustomerId = SeedData.Guid("000101");
    public static readonly Guid PendingOrderId = SeedData.Guid("000305");
    public static readonly Guid ShippedOrderId = SeedData.Guid("000303");
    public static readonly Guid ProductId = SeedData.Guid("000201");
    public static readonly Guid SecondProductId = SeedData.Guid("000202");
}
