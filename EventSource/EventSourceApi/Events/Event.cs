using EventSourceApi.Aggregates;

namespace EventSourceApi.Events;

public abstract record Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    //public abstract string AggregateType { get; }
    public Guid AggregateId { get; protected set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType => GetType().Name;
}

public abstract record SupplierEvent : Event
{
    public Guid SupplierId { get; set; }
    //public override string AggregateType => SupplierAggregate.AggregateType;

    public SupplierEvent(Guid supplierId)
    {
        // Required for deserialization
        SupplierId = supplierId;
        AggregateId = supplierId;
    }

}
