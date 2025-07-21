using EventSourceApi.Events;

namespace EventSourceApi.Aggregates;

public sealed class OrderAggregate : AggregateBase<OrderAggregate>
{
    public string Responsible { get; private set; } = "";
    public string Description { get; private set; } = "";
    public List<OrderItem> Items { get; private set; } = [];
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;

    public override void Apply(Event @event)
    {
        switch (@event)
        {
            case OrderCreate create:
                Apply(create);
                break;
            //case SupplierUpdate update:
            //    Apply(update);
            //    break;
            //case SupplierDelete delete:
            //    Apply(delete);
            //    break;
            default:
                break;
        }
    }

    private void Apply(OrderCreate create)
    {
        this.Id = create.OrderId;
        this.Responsible = create.Responsible;
        this.Description = create.Description;
        this.Items = create.Items;
        this.Status = OrderStatus.Pending;
        this.CreatedAt = create.Timestamp;
    }
}

public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Cancelled
}

public struct OrderItem
{
    public string Description { get; set; }
    public float? Quantity { get; set; }
}

