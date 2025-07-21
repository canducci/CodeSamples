using EventSourceApi.Aggregates;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using static EventSourceApi.Endpoints.SupplierEndpoints;

namespace EventSourceApi.Events;

public interface IEventStore
{
    void Append(Event @event);

    void UpdateView<TAggregate>(TAggregate aggregate) where TAggregate : AggregateBase;

    SupplierAggregate? GetSupplierById(Guid id);
    IEnumerable<SupplierAggregate> GetAllSuppliers();
    OrderAggregate? GetOrderById(Guid orderId);
    IEnumerable<OrderAggregate> GetAllOrders();
}

public sealed class InMemoryEventStore : IEventStore
{
    private static readonly ConcurrentBag<Event> _events = [];
    private static readonly ConcurrentDictionary<Guid, AggregateBase> _aggregateViews = new();

    public void Append(Event @event)
    {
        _events.Add(@event);

        AggregateBase? agr = @event switch
        {
            SupplierEvent supplierEvent => GetSupplierById(supplierEvent.AggregateId),
            OrderEvent orderEvent => GetOrderById(orderEvent.AggregateId),
            _ => null
        };

        if (agr != null)
        {
            _aggregateViews.AddOrUpdate(agr.Id, agr, (_, _) => agr);
        }
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
        => ListFromView<SupplierAggregate>();

    public IEnumerable<OrderAggregate> GetAllOrders()
        => ListFromView<OrderAggregate>();

    private static IEnumerable<TAggregate> ListFromView<TAggregate>()
        where TAggregate : AggregateBase
    {
        return _aggregateViews.Values
            .OfType<TAggregate>()
            .OrderBy(a => a.Id)
            .Take(10);
    }

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

    public void UpdateView<TAggregate>(TAggregate aggregate) where TAggregate : AggregateBase
    {

    }
}
