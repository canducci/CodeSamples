using EventSourceApi.Endpoints;
using EventSourceApi.Events;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<IEventStore, EventStore>();

var app = builder.Build();

app.MapSupplierEndpoints();

app.MapOpenApi();
app.MapScalarApiReference();

app.Run();

