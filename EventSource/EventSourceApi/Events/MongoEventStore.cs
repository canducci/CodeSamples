using EventSourceApi.Aggregates;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

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

        BsonClassMap.RegisterClassMap<AggregateBase>(m =>
        {
            m.SetIsRootClass(true);
            m.MapIdProperty(e => e.Id)
                .SetSerializer(guidSerializer);
            m.MapProperty(e => e.CreatedAt);
            m.MapProperty(e => e.DeletedAt);
        });
        BsonClassMap.RegisterClassMap<SupplierAggregate>();
        BsonClassMap.RegisterClassMap<OrderAggregate>();
    }

    public void Append(Event @event)
    {
        mongoCollection
            .InsertOne(@event);
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


        if (aggregate != null)
            UpdateView(aggregate);

        return aggregate;
    }

    private IEnumerable<TAggregate> Replay<TAggregate, TEvent>()
        where TAggregate : AggregateBase<TAggregate>, new()
        where TEvent : Event
    {
        var evs = mongoCollection
            .Find(e => e is TEvent)
            .SortBy(e => e.Timestamp)
            .ToList()
            .GroupBy(e => e.AggregateId)
            .Take(10)
            .ToList();

        foreach (var @event in evs)
        {
            var aggregate = AggregateBase<TAggregate>.Replay(@event);
            if (aggregate == null)
                continue;

            yield return aggregate;
        }
    }

    private void UpdateView<TAggregate>(TAggregate aggregate) where TAggregate : AggregateBase
    {
        var filter = Builders<AggregateBase>.Filter.Eq(a => a.Id, aggregate.Id);
        var update = Builders<AggregateBase>.Update.Set(a => a, aggregate);
        mongoViewsCollection.UpdateOne(filter, update, new UpdateOptions { IsUpsert = true });
    }

    public IEnumerable<SupplierAggregate> GetAllSuppliers()
     => Replay<SupplierAggregate, SupplierEvent>();

    public IEnumerable<OrderAggregate> GetAllOrders()
        => Replay<OrderAggregate, OrderEvent>();

    public SupplierAggregate? GetSupplierById(Guid id)
        => GetAggregateById<SupplierAggregate, SupplierEvent>(id);

    public OrderAggregate? GetOrderById(Guid orderId)
        => GetAggregateById<OrderAggregate, OrderEvent>(orderId);
}
