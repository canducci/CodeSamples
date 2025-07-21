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

    builder.Services.AddScoped(
        s =>
        {
            var client = new MongoClient(mongo);
            var database = client.GetDatabase("EventSourceDb");
            return database.GetCollection<Event>("Suppliers");
        });

    builder.Services.AddScoped<IEventStore, MongoEventStore>();
    MongoEventStore.Configure();
}


var app = builder.Build();


app.MapSupplierEndpoints();

app.MapOpenApi();

app.MapScalarApiReference();

app.Run();

