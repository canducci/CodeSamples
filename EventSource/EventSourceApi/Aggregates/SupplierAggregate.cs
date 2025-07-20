using EventSourceApi.Events;

namespace EventSourceApi.Aggregates;

public class SupplierAggregate
{
    public const string AggregateType = "Supplier";

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string ContactEmail { get; private set; }
    public string ContactPhone { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private SupplierAggregate()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        // Required for deserialization
    }

    private SupplierAggregate(Guid id, string name, string contactEmail, string contactPhone)
    {
        Id = id;
        Name = name;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
    }

    private void Apply(SupplierCreate create)
    {
        this.Id = create.SupplierId;
        this.Name = create.Name;
        this.ContactEmail = create.ContactEmail;
        this.ContactPhone = create.ContactPhone;
    }

    private void Apply(SupplierUpdate updated)
    {
        if(updated.Name != null)
            this.Name = updated.Name;
        if(updated.ContactEmail != null)
            this.ContactEmail = updated.ContactEmail;
        if(updated.ContactPhone != null)
            this.ContactPhone = updated.ContactPhone;
    }

    private void Apply(object unknouEvent)
    {
        // Handle unknown events if necessary
    }

    public void Apply(Event @event)
    {
        Apply((dynamic)@event);
    }

    public static SupplierAggregate Materialize(IEnumerable<Event> events)
    {
        var supplier = new SupplierAggregate();
        foreach (var @event in events)
        {
            supplier.Apply(@event);
        }
        return supplier;

    }
}
