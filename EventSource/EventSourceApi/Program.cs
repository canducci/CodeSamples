using EventSourceApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapSupplierEndpoints();

app.Run();

