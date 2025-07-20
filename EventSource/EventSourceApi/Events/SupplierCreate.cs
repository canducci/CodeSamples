using EventSourceApi.Aggregates;

namespace EventSourceApi.Events;

public record SupplierCreate : Event
{
    public Guid SupplierId { get; private set; }
    public string Name { get; private set; }
    public string ContactEmail { get; private set; }
    public string ContactPhone { get; private set; }
    public override Guid AggregateId => SupplierId;
    public override string AggregateType => SupplierAggregate.AggregateType;
}
