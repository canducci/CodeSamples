using EventSourceApi.Aggregates;
using System.Numerics;

namespace EventSourceApi.Events;


public interface IEventStore
{
    void Append(Event @event);
    SupplierAggregate? GetSupplierById(Guid id);
    IEnumerable<SupplierAggregate> GetAllSuppliers();
    OrderAggregate? GetOrderById(Guid orderId);
    IEnumerable<OrderAggregate> GetAllOrders();
}

public sealed class EventStore : IEventStore
{
    private static readonly List<Event> _events = [];

    public void Append(Event @event)
    {
        _events.Add(@event);
    }

    public SupplierAggregate? GetSupplierById(Guid supplierId)
    {
        var supplierEvents = _events
            .Where(e => e is SupplierEvent && e.AggregateId == supplierId)
            .OrderBy(e => e.Timestamp)
            .ToList();

        if (supplierEvents.Count == 0)
            return null;

        return SupplierAggregate.Replay(supplierEvents);
    }

    public IEnumerable<SupplierAggregate> GetAllSuppliers()
        => Replay<SupplierAggregate, SupplierEvent>();

    public IEnumerable<OrderAggregate> GetAllOrders()
        => Replay<OrderAggregate, OrderEvent>();

    private static IEnumerable<TAggregate> Replay<TAggregate, TEvent>()
        where TAggregate : AggregateBase<TAggregate>, new()
        where TEvent : Event
    {

        var tEvents = _events
            .Where(e => e is TEvent)
            .OrderBy(e => e.Timestamp)
            .GroupBy(e => e.AggregateId)
            .Take(10)
            .ToList();

        foreach (var @event in tEvents)
        {
            var aggregate = AggregateBase<TAggregate>.Replay(@event);
            if (aggregate == null)
                continue;
            yield return aggregate;
        }

    }

    public OrderAggregate? GetOrderById(Guid orderId)
    {
        var supplierEvents = _events
            .Where(e => e is OrderEvent && e.AggregateId == orderId)
            .OrderBy(e => e.Timestamp)
            .ToList();

        if (supplierEvents.Count == 0)
            return null;

        return OrderAggregate.Replay(supplierEvents);
    }
}
