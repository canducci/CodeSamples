using EventSourceApi.Aggregates;

namespace EventSourceApi.Events;

public abstract record OrderEvent : Event
{
    public Guid OrderId => AggregateId;
    public OrderEvent(Guid orderId)
    {
        AggregateId = orderId;
    }
}

public sealed record OrderCreate(Guid OrderId, string Responsible, string Description, List<OrderItem> Items) : OrderEvent(OrderId);