using EventSourceApi.Aggregates;

namespace EventSourceApi.Events;

public record SupplierUpdate(Guid SupplierId) : SupplierEvent(SupplierId)
{
    public string? Name { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}
