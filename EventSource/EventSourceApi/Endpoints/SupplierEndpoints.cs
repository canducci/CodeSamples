using EventSourceApi.Aggregates;
using EventSourceApi.Events;
using System.Net.NetworkInformation;

namespace EventSourceApi.Endpoints;

public static class SupplierEndpoints
{
    public static void MapSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        var appGroup = app.MapGroup("/suppliers");

        appGroup.MapGet("/", (IEventStore eventStore) =>
        {
            var suppliers = eventStore.GetAllSuppliers()
                .Select(x => new Supplier(x.Id, x.Name, x.ContactEmail, x.ContactPhone));
            return Results.Ok(suppliers);
        })
            .Produces<IEnumerable<Supplier>>(200);

        appGroup.MapGet("/{supplierId}", (IEventStore eventStore, Guid supplierId) =>
        {
            var supplier = eventStore.GetSupplierById(supplierId);
            if (supplier == null)
                return Results.NotFound();
            return Results.Ok(new Supplier(supplier.Id, supplier.Name, supplier.ContactEmail, supplier.ContactPhone);
        })
            .Produces<Supplier>(200)
            .Produces(404);

        appGroup.MapPost("/", (IEventStore eventStore, SupplierPostRequest create) =>
        {
            var createRequestEvent = new SupplierCreate
            {
                SupplierId = Guid.NewGuid(),
                Name = create.Name,
                ContactEmail = create.ContactEmail,
                ContactPhone = create.ContactPhone
            };

            eventStore.Append(createRequestEvent);
            var supplier = eventStore.GetSupplierById(createRequestEvent.SupplierId);

            return Results.Created($"/suppliers/{createRequestEvent.SupplierId}", 
                new Supplier(supplier.Id, supplier.Name, supplier.ContactEmail, supplier.ContactPhone));
        })
            .Produces<Supplier>(201);

        appGroup.MapPut("/{supplierId}", (IEventStore eventStore, Guid supplierId, SupplierPutRequest update) =>
        {
            var supplier = eventStore.GetSupplierById(supplierId);
            if (supplier == null)
                return Results.BadRequest();

            var @event = new SupplierUpdate
            {
                SupplierId = supplierId,
                ContactEmail = update.ContactEmail,
                ContactPhone = update.ContactPhone
            };

            eventStore.Append(@event);
            supplier.Apply(@event);
            return Results.Ok(new Supplier(supplier.Id, supplier.Name, supplier.ContactEmail, supplier.ContactPhone));
        })
            .Produces<Supplier>(200)
            .Produces(400);
    }
    public record struct SupplierPostRequest(string Name, string ContactEmail, string ContactPhone);
    public record struct SupplierPutRequest(string? ContactEmail, string? ContactPhone);
    public record struct Supplier(Guid Id, string Name, string ContactEmail, string ContactPhone);

}
