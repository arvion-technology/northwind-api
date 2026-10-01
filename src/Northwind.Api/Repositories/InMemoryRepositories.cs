namespace Northwind.Api.Repositories;

using Northwind.Api.Models;

/// <summary>
/// In-memory repositories behind interfaces. The API only needs read, add and
/// replace, so the interface stays small enough to swap for EF Core later.
/// Every method takes a lock: the demo server handles concurrent requests.
/// </summary>
public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly List<Customer> _customers;
    private readonly object _sync = new();

    public InMemoryCustomerRepository(IEnumerable<Customer>? seed = null)
        => _customers = (seed ?? SeedData.Customers()).ToList();

    public IReadOnlyList<Customer> GetAll()
    {
        lock (_sync) return _customers.ToList();
    }

    public Customer? GetById(Guid id)
    {
        lock (_sync) return _customers.FirstOrDefault(customer => customer.Id == id);
    }

    public Customer Add(Customer customer)
    {
        lock (_sync)
        {
            _customers.Add(customer);
            return customer;
        }
    }

    public bool EmailExists(string email)
    {
        lock (_sync) return _customers.Any(customer =>
            string.Equals(customer.Email, email, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly List<Order> _orders;
    private readonly object _sync = new();

    public InMemoryOrderRepository(IEnumerable<Order>? seed = null)
        => _orders = (seed ?? SeedData.Orders()).ToList();

    public IReadOnlyList<Order> GetAll()
    {
        lock (_sync) return _orders.ToList();
    }

    public Order? GetById(Guid id)
    {
        lock (_sync) return _orders.FirstOrDefault(order => order.Id == id);
    }

    public Order Add(Order order)
    {
        lock (_sync)
        {
            _orders.Add(order);
            return order;
        }
    }

    public Order Replace(Order order)
    {
        lock (_sync)
        {
            var index = _orders.FindIndex(existing => existing.Id == order.Id);
            if (index == -1) throw new InvalidOperationException($"Order {order.Id} is not in the repository.");
            _orders[index] = order;
            return order;
        }
    }
}
