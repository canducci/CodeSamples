namespace EventSourceApi.Events;

public abstract record Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public abstract string AggregateType { get; }
    public abstract Guid AggregateId { get; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType => GetType().Name;


}
