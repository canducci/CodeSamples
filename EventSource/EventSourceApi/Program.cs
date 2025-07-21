using EventSourceApi.Aggregates;
using EventSourceApi.Endpoints;
using EventSourceApi.Events;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var mongo = builder.Configuration.GetConnectionString("MongoDb");

if (string.IsNullOrWhiteSpace(mongo))
{
    builder.Services.AddScoped<IEventStore, EventStore>();
}
else
{
    var client = new MongoClient(mongo);
    builder.Services.AddScoped(
        s =>
        {
            var database = client.GetDatabase("EventSourceDb");
            return database.GetCollection<Event>("Events");
        });

    builder.Services.AddScoped(
        s =>
        {
            var database = client.GetDatabase("EventSourceDb");
            return database.GetCollection<AggregateBase>("Views");
        });

    builder.Services.AddScoped<IEventStore, MongoEventStore>();
    MongoEventStore.Configure();
}

var app = builder.Build();

app.MapSupplierEndpoints();
app.MapOrderEndpoints();

app.MapOpenApi();

app.MapScalarApiReference();

app.Run();

