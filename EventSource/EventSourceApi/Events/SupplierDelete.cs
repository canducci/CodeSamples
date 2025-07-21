
using EventSourceApi.Aggregates;

namespace EventSourceApi.Events;

public record SupplierDelete(Guid SupplierId) : SupplierEvent(SupplierId)
{
}
