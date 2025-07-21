using EventSourceApi.Aggregates;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;

namespace EventSourceApi.Events;

public class MongoEventStore(IMongoCollection<Event> mongoCollection, IMongoCollection<AggregateBase> mongoViewsCollection) : IEventStore
{
    public static void Configure()
    {
        var guidSerializer = new GuidSerializer(GuidRepresentation.Standard);

        BsonClassMap.RegisterClassMap<Event>(e =>
        {
            e.SetIsRootClass(true);
            e.MapIdProperty(e => e.Id)
                .SetSerializer(guidSerializer);
            e.MapProperty(e => e.AggregateId)
                .SetSerializer(guidSerializer);
            e.MapProperty(m => m.Timestamp);
        });

        BsonClassMap.RegisterClassMap<SupplierEvent>(m =>
        {
            m.SetIsRootClass(true);
            m.UnmapProperty(e => e.SupplierId);
        });

        BsonClassMap.RegisterClassMap<SupplierCreate>();
        BsonClassMap.RegisterClassMap<SupplierUpdate>();
        BsonClassMap.RegisterClassMap<SupplierDelete>();

        BsonClassMap.RegisterClassMap<OrderEvent>(m =>
        {
            m.SetIsRootClass(true);
            m.UnmapProperty(e => e.OrderId);
        });
        BsonClassMap.RegisterClassMap<OrderCreate>();
        BsonClassMap.RegisterClassMap<OrderUpdate>();
        BsonClassMap.RegisterClassMap<OrderDelete>();

        BsonClassMap.RegisterClassMap<AggregateBase>(m =>
        {
            m.SetIsRootClass(true);
            m.MapIdProperty(e => e.Id)
                .SetSerializer(guidSerializer);
            m.AutoMap();
        });
        BsonClassMap.RegisterClassMap<SupplierAggregate>(m =>
        {
            m.SetIsRootClass(true);
            m.AutoMap();
            //m.MapProperty(e => e.Name);
            //m.MapProperty(e => e.ContactEmail);
            //m.MapProperty(e => e.ContactPhone);

        });
        BsonClassMap.RegisterClassMap<OrderAggregate>(m =>
        {
            m.SetIsRootClass(true);
            m.AutoMap();
            //m.MapProperty(e => e.Responsible);
            //m.MapProperty(e => e.Description);
            //m.MapProperty(e => e.Items);
            //m.MapProperty(e => e.Status);
        });
    }

    public void Append(Event @event)
    {
        mongoCollection
            .InsertOne(@event);

        AggregateBase? aggregate = @event switch
        {
            SupplierEvent supplierEvent => GetAggregateById<SupplierAggregate, SupplierEvent>(supplierEvent.AggregateId),
            OrderEvent orderEvent => GetAggregateById<OrderAggregate, OrderEvent>(orderEvent.AggregateId),
            _ => throw new InvalidOperationException("Unknown event type")
        };

        if (aggregate == null)
            throw new InvalidOperationException("Aggregate not found for event");

        UpdateView(aggregate);
    }

    private TAggregate? GetAggregateById<TAggregate, TEvent>(Guid id)
        where TAggregate : AggregateBase<TAggregate>, new()
    {
        var aggregateEvents = mongoCollection
            .Find(e => e is TEvent && e.AggregateId == id)
            .SortBy(e => e.Timestamp)
            .ToList();

        if (aggregateEvents.Count == 0)
            return null;

        var aggregate = AggregateBase<TAggregate>.Replay(aggregateEvents);

        return aggregate;
    }

    private IEnumerable<TAggregate> GetFromView<TAggregate>()
    {
        return
            mongoViewsCollection.Find(f => f is TAggregate)
                .ToList().Cast<TAggregate>();
    }

    public IEnumerable<SupplierAggregate> GetAllSuppliers()
        => GetFromView<SupplierAggregate>();

    public IEnumerable<OrderAggregate> GetAllOrders()
        => GetFromView<OrderAggregate>();

    public SupplierAggregate? GetSupplierById(Guid id)
        => GetAggregateById<SupplierAggregate, SupplierEvent>(id);

    public OrderAggregate? GetOrderById(Guid orderId)
        => GetAggregateById<OrderAggregate, OrderEvent>(orderId);

    public void UpdateView<TAggregate>(TAggregate aggregate) where TAggregate : AggregateBase
    {
        var filter = Builders<AggregateBase>.Filter.Eq(a => a.Id, aggregate.Id);
        var t = mongoViewsCollection.FindOneAndReplace(filter, aggregate, new() { IsUpsert = true });
    }
}
