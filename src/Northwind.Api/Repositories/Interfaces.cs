namespace Northwind.Api.Repositories;

using Northwind.Api.Models;

public interface ICustomerRepository
{
    IReadOnlyList<Customer> GetAll();
    Customer? GetById(Guid id);
    Customer Add(Customer customer);
    bool EmailExists(string email);
}

public interface IOrderRepository
{
    IReadOnlyList<Order> GetAll();
    Order? GetById(Guid id);
    Order Add(Order order);
    Order Replace(Order order);
}
