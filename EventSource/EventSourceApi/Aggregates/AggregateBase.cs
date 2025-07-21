using EventSourceApi.Events;

namespace EventSourceApi.Aggregates;

public abstract class AggregateBase
{
    public Guid Id { get; protected set; }
    public DateTime? CreatedAt { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }
}

public abstract class AggregateBase<T> : AggregateBase
    where T : AggregateBase<T>, new()
{
    public abstract void Apply(Event @event);

    public static T? Replay(IEnumerable<Event> events)
    {
        if (events == null || !events.Any())
            return null;

        var aggregate = new T();
        foreach (var @event in events)
        {
            aggregate.Apply(@event);
        }
        return aggregate;
    }
}
