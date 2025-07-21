using EventSourceApi.Events;

namespace EventSourceApi.Aggregates;

public sealed class SupplierAggregate : AggregateBase<SupplierAggregate>
{
    public string Name { get; private set; } = "";
    public string ContactEmail { get; private set; } = "";
    public string ContactPhone { get; private set; } = "";
    

    public override void Apply(Event @event)
    {
        switch (@event)
        {
            case SupplierCreate create:
                Apply(create);
                break;
            case SupplierUpdate update:
                Apply(update);
                break;
            case SupplierDelete delete:
                Apply(delete);
                break;
            default:
                break;
        }
    }

    private void Apply(SupplierCreate create)
    {
        this.Id = create.SupplierId;
        this.Name = create.Name;
        this.ContactEmail = create.ContactEmail;
        this.ContactPhone = create.ContactPhone;
        this.CreatedAt = create.Timestamp;
    }

    private void Apply(SupplierUpdate updated)
    {
        if (updated.Name != null)
            this.Name = updated.Name;
        if (updated.ContactEmail != null)
            this.ContactEmail = updated.ContactEmail;
        if (updated.ContactPhone != null)
            this.ContactPhone = updated.ContactPhone;
    }

    private void Apply(SupplierDelete deleted)
    {
        DeletedAt = deleted.Timestamp;
    }

    
}
