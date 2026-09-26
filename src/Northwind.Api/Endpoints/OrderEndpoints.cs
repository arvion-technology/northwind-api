namespace Northwind.Api.Endpoints;

using Northwind.Api.Contracts;
using Northwind.Api.Http;
using Northwind.Api.Services;
using Northwind.Api.Validation;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");
        group.MapGet("", List).WithName("ListOrders");
        group.MapGet("/{id:guid}", GetById).WithName("GetOrder");
        group.MapPost("", Create).WithName("CreateOrder");
        group.MapPost("/{id:guid}/cancel", Cancel).WithName("CancelOrder");
        return app;
    }

    public static IResult List(
        OrderService service,
        Guid? customerId = null,
        string? status = null,
        int page = 1,
        int pageSize = 10,
        string? sort = null,
        string? order = null)
    {
        var errors = RequestValidator.Merge(
            RequestValidator.ValidatePaging(page, pageSize),
            RequestValidator.ValidateQuery(sort, order, RequestValidator.OrderSorts));

        if (!string.IsNullOrWhiteSpace(status) && !RequestValidator.TryParseStatus(status, out _))
            errors["status"] = new[] { "Status must be one of: pending, paid, shipped, delivered, cancelled." };

        if (errors.Count > 0) return ApiProblem.Validation(errors);

        var result = service.List(
            customerId,
            status,
            page,
            pageSize,
            sort ?? "placedAt",
            order ?? "desc");
        return TypedResults.Ok(result);
    }

    public static IResult GetById(Guid id, OrderService service)
    {
        var order = service.Get(id);
        return order is null
            ? ApiProblem.NotFound($"No order with id {id}.")
            : TypedResults.Ok(order);
    }

    public static IResult Create(CreateOrderRequest request, OrderService orders, CustomerService customers)
    {
        var errors = RequestValidator.ValidateCreateOrder(request);
        if (errors.Count > 0) return ApiProblem.Validation(errors);

        if (customers.Get(request.CustomerId) is null)
            return ApiProblem.NotFound($"No customer with id {request.CustomerId}.");

        var created = orders.Create(request.CustomerId, request.Lines!);
        return TypedResults.Created($"/api/orders/{created.Id}", created);
    }

    public static IResult Cancel(Guid id, OrderService service)
    {
        var result = service.Cancel(id);
        return result.Outcome switch
        {
            CancelOutcome.NotFound => ApiProblem.NotFound($"No order with id {id}."),
            CancelOutcome.NotCancellable => ApiProblem.Conflict(
                $"Order {id} is {result.Order!.Status} and can no longer be cancelled. Only pending orders can be cancelled."),
            _ => TypedResults.Ok(result.Order),
        };
    }
}
