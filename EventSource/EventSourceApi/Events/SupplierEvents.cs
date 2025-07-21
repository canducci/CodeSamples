using EventSourceApi.Aggregates;

namespace EventSourceApi.Events;

public abstract record SupplierEvent : Event
{
    public Guid SupplierId => AggregateId;
    public SupplierEvent(Guid supplierId)
    {
        AggregateId = supplierId;
    }
}

public record SupplierCreate(Guid SupplierId, string Name, string ContactEmail, string ContactPhone) : SupplierEvent(SupplierId);

public record SupplierDelete(Guid SupplierId) : SupplierEvent(SupplierId);

public record SupplierUpdate(Guid SupplierId, string? Name, string? ContactEmail, string? ContactPhone) : SupplierEvent(SupplierId);