using EventSourceApi.Aggregates;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace EventSourceApi.Events;

public class MongoEventStore : IEventStore
{
    private readonly IMongoCollection<Event> mongoCollection;
    public static void Configure()
    {
        BsonClassMap.RegisterClassMap<Event>(e =>
        {
            e.SetIsRootClass(true);
            e.MapIdProperty(e => e.Id)
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            e.MapProperty(e => e.AggregateId)
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            e.MapProperty(m => m.Timestamp);
        });

        BsonClassMap.RegisterClassMap<SupplierEvent>(m =>
        {
            m.MapProperty(m => m.SupplierId)
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
        });
        BsonClassMap.RegisterClassMap<SupplierCreate>();
        BsonClassMap.RegisterClassMap<SupplierUpdate>();
        BsonClassMap.RegisterClassMap<SupplierDelete>();
    }


    public MongoEventStore(IMongoCollection<Event> mongoCollection)
    {
        this.mongoCollection = mongoCollection;
    }

    public void Append(Event @event)
    {
        mongoCollection
            .InsertOne(@event);
    }

    public IEnumerable<SupplierAggregate> GetAllSuppliers()
    {
        var evs = mongoCollection.AsQueryable()
            .Where(e => e is SupplierEvent)
            .OrderBy(e => e.Timestamp)
            .GroupBy(e => e.AggregateId)
            .Take(10)
            .ToList();

        var suppliers = evs.Select(SupplierAggregate.Materialize)
            .ToList();

        return suppliers;
    }

    public SupplierAggregate? GetSupplierById(Guid id)
    {
        //var supplierEvents = mongoCollection.AsQueryable()
        //    .Where(e=>e is SupplierEvent)
        //    .OrderBy(e => e.Timestamp)
        //    .Where(e => e.AggregateId == id)
        //    .ToArray();

        var supplierEvents = mongoCollection
            .Find(e => e is SupplierEvent && e.AggregateId == id)
            .SortBy(e => e.Timestamp)
            .ToList();

        if (!supplierEvents.Any())
            return null;


        var supplier = SupplierAggregate.Materialize(supplierEvents);

        return supplier;

    }
}
