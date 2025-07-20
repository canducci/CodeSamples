using EventSourceApi.Events;

namespace EventSourceApi.Endpoints;

public static class SupplierEndpoints
{
    public static void MapSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        var appGroup = app.MapGroup("/suppliers");

        appGroup.MapGet("/", (EventStore eventStore) =>
        {
            var suppliers = eventStore.GetAllSuppliers();
            return Results.Ok(suppliers);
        });

        appGroup.MapGet("/{id}", (EventStore eventStore, Guid id) =>
        {
            var supplier = eventStore.GetSupplierById(id);
            if (supplier == null)
                return Results.NotFound();
            return Results.Ok(supplier);
        });

        appGroup.MapPost("/", (EventStore eventStore, SupplierCreate create) =>
        {   
            eventStore.Append(create);
            var supplier = eventStore.GetSupplierById(create.SupplierId);
            return Results.Created($"/suppliers/{create.SupplierId}", supplier);
        });

        appGroup.MapPut("/{id}", async (EventStore eventStore, Guid id, SupplierUpdated updated) =>
        {
            updated.SupplierId = id;

            var supplier = eventStore.GetSupplierById(id);
            if (supplier == null)
                return Results.BadRequest();
            eventStore.Append(updated);
            return Results.Accepted();
        });
    }
}
