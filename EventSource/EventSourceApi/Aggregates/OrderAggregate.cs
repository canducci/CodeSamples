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
            case OrderUpdate update:
                Apply(update);
                break;
            case OrderDelete delete:
                Apply(delete);
                break;
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
    private void Apply(OrderUpdate update)
    {
        if (update.Responsible != null)
            this.Responsible = update.Responsible;
        if (update.Description != null)
            this.Description = update.Description;
        if (update.ItemsToRemove != null && update.ItemsToRemove.Count != 0)
        {
            foreach (var item in update.ItemsToRemove)
            {
                Items.RemoveAll(i => i.Description == item.Description);
            }
        }
        if (update.ItemsToAdd != null && update.ItemsToAdd.Count != 0)
        {
            foreach (var item in update.ItemsToAdd)
            {
                Items.Add(item);
            }
        }
    }
    private void Apply(OrderDelete delete)
    {
        DeletedAt = delete.Timestamp;
    }
}

public enum OrderStatus
{
    Pending,
    OpenForQuotation,
    Approved,
    Cancelled
}

public struct OrderItem
{
    public string Description { get; set; }
    public float? Quantity { get; set; }
}

