using EventSourceApi.Aggregates;
using EventSourceApi.Events;
using System.Net.NetworkInformation;

namespace EventSourceApi.Endpoints;

public static class SupplierEndpoints
{
    public static void MapSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        var appGroup = app.MapGroup("/suppliers")
            .RequireAuthorization();

        appGroup.MapGet("/", (IEventStore eventStore) =>
        {
            var suppliers = eventStore.GetAllSuppliers()
                .Select(x => new Supplier(x.Id, x.Name, x.ContactEmail, x.ContactPhone, x.DeletedAt.HasValue));
            return Results.Ok(suppliers);
        })
            .Produces<IEnumerable<Supplier>>(200);

        appGroup.MapGet("/{supplierId:guid}", (IEventStore eventStore, Guid supplierId) =>
        {
            var supplier = eventStore.GetSupplierById(supplierId);
            if (supplier == null)
                return Results.NotFound();
            return Results.Ok(new Supplier(supplier.Id, supplier.Name, supplier.ContactEmail, supplier.ContactPhone, supplier.DeletedAt.HasValue)); // changed x to supplier
        })
            .Produces<Supplier>(200)
            .Produces(400)
            .Produces(404);

        appGroup.MapPost("/", (IEventStore eventStore, SupplierPostRequest create) =>
        {
            var createRequestEvent = new SupplierCreate(Guid.NewGuid(), create.Name, create.ContactEmail, create.ContactPhone);

            eventStore.Append(createRequestEvent);

            var supplier = eventStore.GetSupplierById(createRequestEvent.SupplierId);

            if (supplier == null)
                return Results.BadRequest();

            return Results.Created($"/suppliers/{createRequestEvent.SupplierId}",
                new Supplier(supplier.Id, supplier.Name, supplier.ContactEmail, supplier.ContactPhone, supplier.DeletedAt.HasValue));
        })
            .Produces<Supplier>(201)
            .Produces(400); 

        appGroup.MapPut("/{supplierId:guid}", (IEventStore eventStore, Guid supplierId, SupplierPutRequest update) =>
        {
            var supplier = eventStore.GetSupplierById(supplierId);
            if (supplier == null)
                return Results.BadRequest();

            var @event = new SupplierUpdate(supplierId, null, update.ContactEmail, update.ContactPhone);

            eventStore.Append(@event);
            supplier.Apply(@event);

            return Results.Ok(new Supplier(supplier.Id, supplier.Name, supplier.ContactEmail, supplier.ContactPhone, supplier.DeletedAt.HasValue)); // updated to include IsDeleted
        })
            .Produces<Supplier>(200)
            .Produces(400);

        appGroup.MapDelete("/{supplierId:guid}", (IEventStore eventStore, Guid supplierId) =>
        {
            var supplier = eventStore.GetSupplierById(supplierId);
            if (supplier == null)
                return Results.NotFound();

            var deleteEvent = new SupplierDelete(supplierId);
            eventStore.Append(deleteEvent);

            return Results.NoContent();
        })
            .Produces(204)
            .Produces(404);
    }
    public record struct SupplierPostRequest(string Name, string ContactEmail, string ContactPhone);
    public record struct SupplierPutRequest(string? ContactEmail, string? ContactPhone);
    public record struct Supplier(Guid Id, string Name, string ContactEmail, string ContactPhone, bool IsDeleted = false);

}
