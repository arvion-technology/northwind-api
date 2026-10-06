namespace Northwind.Api.Services;

using Northwind.Api.Contracts;
using Northwind.Api.Models;
using Northwind.Api.Repositories;
using Northwind.Api.Validation;

public sealed class CustomerService
{
    private readonly ICustomerRepository _repository;

    public CustomerService(ICustomerRepository repository) => _repository = repository;

    public PagedResult<CustomerResponse> List(
        string? search, int page, int pageSize, string sort = "createdAt", string order = "asc")
    {
        IEnumerable<Customer> query = _repository.GetAll();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim();
            query = query.Where(customer =>
                customer.Name.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || customer.Email.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || customer.Country.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        var descending = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);
        IOrderedEnumerable<Customer> ordered = (sort.ToLowerInvariant(), descending) switch
        {
            ("name", true) => query.OrderByDescending(customer => customer.Name, StringComparer.OrdinalIgnoreCase),
            ("name", false) => query.OrderBy(customer => customer.Name, StringComparer.OrdinalIgnoreCase),
            ("country", true) => query.OrderByDescending(customer => customer.Country, StringComparer.OrdinalIgnoreCase),
            ("country", false) => query.OrderBy(customer => customer.Country, StringComparer.OrdinalIgnoreCase),
            ("tier", true) => query.OrderByDescending(customer => customer.Tier, StringComparer.OrdinalIgnoreCase),
            ("tier", false) => query.OrderBy(customer => customer.Tier, StringComparer.OrdinalIgnoreCase),
            (_, true) => query.OrderByDescending(customer => customer.CreatedAt),
            _ => query.OrderBy(customer => customer.CreatedAt),
        };

        var all = ordered.ThenBy(customer => customer.Id).ToList();
        var total = all.Count;
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(customer => customer.ToResponse())
            .ToList();

        return new PagedResult<CustomerResponse>(items, page, pageSize, total, totalPages);
    }

    public CustomerResponse? Get(Guid id) => _repository.GetById(id)?.ToResponse();

    public bool EmailExists(string email) => _repository.EmailExists(email);

    public CustomerResponse Create(string name, string email, string country, string tier)
    {
        var customer = new Customer(
            Guid.NewGuid(),
            name.Trim(),
            email.Trim().ToLowerInvariant(),
            country.Trim(),
            tier.Trim().ToLowerInvariant(),
            DateTimeOffset.UtcNow);
        _repository.Add(customer);
        return customer.ToResponse();
    }
}

public enum CancelOutcome
{
    Cancelled,
    NotFound,
    NotCancellable
}

public sealed record CancelResult(CancelOutcome Outcome, OrderResponse? Order);

public sealed class OrderService
{
    private readonly IOrderRepository _orders;

    public OrderService(IOrderRepository orders) => _orders = orders;

    public PagedResult<OrderResponse> List(
        Guid? customerId,
        string? status,
        int page,
        int pageSize,
        string sort = "placedAt",
        string order = "desc")
    {
        IEnumerable<Order> query = _orders.GetAll();

        if (customerId is { } id)
            query = query.Where(entry => entry.CustomerId == id);

        if (RequestValidator.TryParseStatus(status, out var parsedStatus) && !string.IsNullOrWhiteSpace(status))
            query = query.Where(entry => entry.Status == parsedStatus);

        var descending = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);
        IOrderedEnumerable<Order> ordered = (sort.ToLowerInvariant(), descending) switch
        {
            ("total", true) => query.OrderByDescending(entry => entry.Total),
            ("total", false) => query.OrderBy(entry => entry.Total),
            ("status", true) => query.OrderByDescending(entry => entry.Status.ToString()),
            ("status", false) => query.OrderBy(entry => entry.Status.ToString()),
            (_, true) => query.OrderByDescending(entry => entry.PlacedAt),
            _ => query.OrderBy(entry => entry.PlacedAt),
        };

        var all = ordered.ThenBy(entry => entry.Id).ToList();
        var total = all.Count;
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entry => entry.ToResponse())
            .ToList();

        return new PagedResult<OrderResponse>(items, page, pageSize, total, totalPages);
    }

    public OrderResponse? Get(Guid id) => _orders.GetById(id)?.ToResponse();

    public OrderResponse Create(Guid customerId, IReadOnlyList<CreateOrderLine> lines)
    {
        var orderLines = lines
            .Select(line => new OrderLine(
                line.ProductId,
                line.ProductName!.Trim(),
                line.Quantity,
                decimal.Round(line.UnitPrice, 2)))
            .ToList();

        var order = new Order(
            Guid.NewGuid(),
            customerId,
            orderLines,
            OrderStatus.Pending,
            DateTimeOffset.UtcNow);
        _orders.Add(order);
        return order.ToResponse();
    }

    /// <summary>Pending orders can be cancelled. Paid, shipped, delivered and
    /// already-cancelled orders cannot, and the API returns a 409 for those.</summary>
    public CancelResult Cancel(Guid id)
    {
        var order = _orders.GetById(id);
        if (order is null) return new CancelResult(CancelOutcome.NotFound, null);
        if (order.Status != OrderStatus.Pending)
            return new CancelResult(CancelOutcome.NotCancellable, order.ToResponse());

        var cancelled = order with { Status = OrderStatus.Cancelled };
        _orders.Replace(cancelled);
        return new CancelResult(CancelOutcome.Cancelled, cancelled.ToResponse());
    }
}
