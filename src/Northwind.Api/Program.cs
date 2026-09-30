using Northwind.Api.Endpoints;
using Northwind.Api.Repositories;
using Northwind.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// In-memory repositories behind interfaces: the sample has no database. Each
// repository is a singleton so the seed data survives every request.
builder.Services.AddSingleton<ICustomerRepository>(_ => new InMemoryCustomerRepository());
builder.Services.AddSingleton<IOrderRepository>(_ => new InMemoryOrderRepository());
builder.Services.AddSingleton<CustomerService>();
builder.Services.AddSingleton<OrderService>();

var app = builder.Build();

app.MapMetaEndpoints();
app.MapCustomerEndpoints();
app.MapOrderEndpoints();

app.Run();

// Exposed so a future integration test can use WebApplicationFactory<Program>.
public partial class Program;
