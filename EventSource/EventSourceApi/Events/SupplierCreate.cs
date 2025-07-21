using EventSourceApi.Aggregates;
using static EventSourceApi.Endpoints.SupplierEndpoints;

namespace EventSourceApi.Events;

public record SupplierCreate(Guid SupplierId) : SupplierEvent(SupplierId)
{
    public string Name { get;  set; }
    public string ContactEmail { get;  set; }
    public string ContactPhone { get;  set; }
}
