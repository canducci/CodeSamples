using EventSourceApi.Aggregates;
using EventSourceApi.Events;
using Microsoft.AspNetCore.Http.HttpResults;
using static EventSourceApi.Endpoints.SupplierEndpoints;

namespace EventSourceApi.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var appGroup = app.MapGroup("/orders")
            .RequireAuthorization();

        appGroup.MapGet("/", (IEventStore eventStore) =>
        {
            var orders = eventStore.GetAllOrders()
                .Select(x => new Order(x.Id, x.Responsible, x.Description, null, x.Status.ToString(), x.CreatedAt ?? DateTime.MinValue));

            return Results.Ok(orders);
        })
            .Produces<IEnumerable<Order>>(200);

        appGroup.MapGet("/{orderId:guid}", (IEventStore eventStore, Guid orderId) =>
        {
            var order = eventStore.GetOrderById(orderId);
            if (order == null)
                return Results.NotFound();

            return Results.Ok(new Order(order.Id,
                          order.Responsible,
                          order.Description,
                          [.. order.Items.Select(oi => new OrderItem(oi.Description, oi.Quantity))],
                          order.Status.ToString(),
                          order.CreatedAt!.Value));
        })
            .Produces<Order>(200)
            .Produces(404);

        appGroup.MapPost("/", (IEventStore eventStore, OrderPostRequest create) =>
        {
            if (create.Items.Count == 0)
                return Results.BadRequest("At least one item is required.");

            var items = create.Items.Select(item => new Aggregates.OrderItem
            {
                Description = item.Description,
                Quantity = item.Quantity
            }).ToList();

            var createRequestEvent = new OrderCreate(Guid.NewGuid(), create.Responsible, create.Description, items);

            eventStore.Append(createRequestEvent);
            var order = eventStore.GetOrderById(createRequestEvent.OrderId);

            if (order == null)
                return Results.BadRequest();

            return Results.Created($"/orders/{createRequestEvent.OrderId}",
                new Order(order.Id,
                          order.Responsible,
                          order.Description,
                          [.. order.Items.Select(oi => new OrderItem(oi.Description, oi.Quantity))],
                          order.Status.ToString(),
                          order.CreatedAt!.Value)
                );
        })
            .Produces<Order>(201)
            .Produces(400);

        appGroup.MapPut("/{orderId:guid}", (IEventStore eventStore, Guid orderId, OrderPutRequest update) =>
        {
            var order = eventStore.GetOrderById(orderId);
            if (order == null)
                return Results.BadRequest();

            var itemsToAdd = update.ItemsToAdd?.Select(item => new Aggregates.OrderItem
            {
                Description = item.Description,
                Quantity = item.Quantity
            }).ToList();

            var itemsToRemove = update.ItemsToRemove?.Select(item => new Aggregates.OrderItem
            {
                Description = item.Description,
                Quantity = item.Quantity
            }).ToList();

            var @event = new OrderUpdate(orderId, update.Responsible, update.Description, itemsToAdd, itemsToRemove);            

            eventStore.Append(@event);
            order.Apply(@event);

            return Results.Ok(new Order(order.Id,
                          order.Responsible,
                          order.Description,
                          [.. order.Items.Select(oi => new OrderItem(oi.Description, oi.Quantity))],
                          order.Status.ToString(),
                          order.CreatedAt!.Value));
        })
            .Produces<Order>(200)
            .Produces(400);

        appGroup.MapDelete("/{orderId:guid}", (IEventStore eventStore, Guid orderId) =>
        {
            var order = eventStore.GetOrderById(orderId);
            if (order == null)
                return Results.BadRequest();

            var @event = new OrderDelete(orderId);
            eventStore.Append(@event);

            return Results.NoContent();
        })
            .Produces(204)
            .Produces(400);
    }

    public record struct OrderPostRequest(string Responsible, string Description, List<OrderItem> Items);
    public record struct OrderPutRequest(string? Responsible, string? Description, List<OrderItem>? ItemsToAdd, List<OrderItem>? ItemsToRemove);
    public record struct Order(Guid Id, string Responsible, string Description, List<OrderItem>? Items, string Status, DateTime CreatedAt);
    public record struct OrderItem(string Description, float? Quantity);


}
