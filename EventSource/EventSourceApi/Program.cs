using EventSourceApi;
using EventSourceApi.Aggregates;
using EventSourceApi.Endpoints;
using EventSourceApi.Events;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using MongoDB.Driver;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddAuthentication(ApiKeyAuthenticationSchemeHandler.ApiKeyScheme)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationSchemeHandler>(ApiKeyAuthenticationSchemeHandler.ApiKeyScheme, null);

builder.Services.AddSingleton<IAuthorizationHandler, RoleResourceAuthorization>();
builder.Services.AddAuthorization(o =>
{
    o.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(new RoleResourceRequirement())
        .Build();
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, concellationToken) =>
    {

        var securityScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "X-Api-Key"
        };
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes.Add(ApiKeyAuthenticationSchemeHandler.ApiKeyScheme, securityScheme);

        return Task.CompletedTask;
    });

    //options.AddScalarTransformers();
});

var mongo = builder.Configuration.GetConnectionString("MongoDb");

if (string.IsNullOrWhiteSpace(mongo))
{
    builder.Services.AddScoped<IEventStore, InMemoryEventStore>();
}
else
{
    var client = new MongoClient(mongo);
    var database = client.GetDatabase("EventSourceDb");
    builder.Services.AddScoped(
        s =>
        {
            database.CreateCollection("Events");
            var col = database.GetCollection<Event>("Events");
            col.Indexes.CreateMany(
            [
                new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Ascending(e => e.Id)),
                new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Combine(
                        Builders<Event>.IndexKeys.Ascending(e => e.AggregateId),
                        Builders<Event>.IndexKeys.Ascending(e => e.Timestamp)
                    ))
            ]);
            return col;
        });

    builder.Services.AddScoped<IMongoCollection<AggregateBase>>(
        s => database.GetCollection<AggregateBase>("Views"));

    builder.Services.AddScoped<IEventStore, MongoEventStore>();
    MongoEventStore.Configure();
}

var app = builder.Build();

app.MapSupplierEndpoints();
app.MapOrderEndpoints();

app.MapOpenApi();

app.MapScalarApiReference(options =>
    options
        .AddApiKeyAuthentication(ApiKeyAuthenticationSchemeHandler.ApiKeyScheme, config => config.Value = "my-api-key")
        .AddPreferredSecuritySchemes(ApiKeyAuthenticationSchemeHandler.ApiKeyScheme)
);

app.Run();

