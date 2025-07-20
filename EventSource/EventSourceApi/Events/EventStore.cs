using EventSourceApi.Aggregates;

namespace EventSourceApi.Events;

public class EventStore
{
    private List<Event> _events = new List<Event>();

    public void Append(Event @event)
    {
        _events.Add(@event);
    }

    public SupplierAggregate? GetSupplierById(Guid id)
    {
        var supplierEvents = _events
            .Where(e => e.AggregateType == SupplierAggregate.AggregateType && e.AggregateId == id)
            .OrderBy(e => e.Timestamp)
            .ToList();

        if (!supplierEvents.Any())
            return null;

        return SupplierAggregate.Materialize(supplierEvents);
    }

    public IEnumerable<SupplierAggregate> GetAllSuppliers()
    {
        var suppliersEvents = _events
            .Where(e => e.AggregateType == SupplierAggregate.AggregateType)
            .OrderBy(e => e.Timestamp)
            .GroupBy(e => e.AggregateId)
            .Take(10)
            .ToList();

        foreach(var events in suppliersEvents)
        {
            var supplier = SupplierAggregate.Materialize(events);
            yield return supplier;
        }
    }
}
