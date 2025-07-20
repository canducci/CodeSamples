using EventSourceApi.Aggregates;

namespace EventSourceApi.Events;

public record SupplierUpdate : Event
{
    public Guid SupplierId { get; set; }
    public string? Name { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public override Guid AggregateId => SupplierId;
    public override string AggregateType => SupplierAggregate.AggregateType;
}
