namespace Northwind.Api.Endpoints;

using Microsoft.AspNetCore.Http.HttpResults;
using Northwind.Api.Contracts;
using Northwind.Api.Http;
using Northwind.Api.Services;
using Northwind.Api.Validation;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers").WithTags("Customers");
        group.MapGet("", List).WithName("ListCustomers");
        group.MapGet("/{id:guid}", GetById).WithName("GetCustomer");
        group.MapPost("", Create).WithName("CreateCustomer");
        group.MapGet("/{id:guid}/orders", ListOrders).WithName("ListCustomerOrders");
        return app;
    }

    public static IResult List(
        CustomerService service,
        int page = 1,
        int pageSize = 10,
        string? search = null,
        string? sort = null,
        string? order = null)
    {
        var errors = RequestValidator.Merge(
            RequestValidator.ValidatePaging(page, pageSize),
            RequestValidator.ValidateQuery(sort, order, RequestValidator.CustomerSorts));
        if (errors.Count > 0) return ApiProblem.Validation(errors);

        var result = service.List(search, page, pageSize, sort ?? "createdAt", order ?? "asc");
        return TypedResults.Ok(result);
    }

    public static IResult GetById(Guid id, CustomerService service)
    {
        var customer = service.Get(id);
        return customer is null
            ? ApiProblem.NotFound($"No customer with id {id}.")
            : TypedResults.Ok(customer);
    }

    public static IResult Create(CreateCustomerRequest request, CustomerService service)
    {
        var errors = RequestValidator.ValidateCreateCustomer(request);
        if (errors.Count > 0) return ApiProblem.Validation(errors);

        if (service.EmailExists(request.Email!.Trim()))
            return ApiProblem.Conflict($"A customer with email {request.Email.Trim()} already exists.");

        var created = service.Create(
            request.Name!,
            request.Email!,
            request.Country!,
            string.IsNullOrWhiteSpace(request.Tier) ? "free" : request.Tier!);
        return TypedResults.Created($"/api/customers/{created.Id}", created);
    }

    public static IResult ListOrders(Guid id, CustomerService customerService, OrderService orderService, int page = 1, int pageSize = 10)
    {
        var errors = RequestValidator.ValidatePaging(page, pageSize);
        if (errors.Count > 0) return ApiProblem.Validation(errors);

        if (customerService.Get(id) is null)
            return ApiProblem.NotFound($"No customer with id {id}.");

        return TypedResults.Ok(orderService.List(id, null, page, pageSize));
    }
}
