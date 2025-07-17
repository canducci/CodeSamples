using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MyWebApi;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

builder.Logging.ClearProviders();
builder.Logging.EnableRedaction();
builder.Services.AddRedaction(configure =>
{   

    configure.SetRedactor<MaskingRedactor>(
        new DataClassificationSet(MyWebApiTaxonomy.SensitiveData)
        );
});

builder.Services.AddMetrics();
builder.Services.AddSingleton<WeatherApiMetrics>();

string serviceName = builder.Environment.ApplicationName;

ResourceBuilder resourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName, "CodeSamples", "v0.1", false, "Demo");

builder.Services.AddOpenTelemetry()
    .WithMetrics(configure =>
    {
        configure.SetResourceBuilder(resourceBuilder);
        configure.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddRuntimeInstrumentation()
               .AddMeter(WeatherApiMetrics.MeterName);

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            configure.AddOtlpExporter();
        }
    })
    .WithTracing(configure =>
    {
        configure.SetResourceBuilder(resourceBuilder);

        configure.SetSampler(new AlwaysOnSampler());

        configure.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddSource(builder.Environment.ApplicationName);

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            configure.AddOtlpExporter();
        }
    });

var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
if (useOtlpExporter)
{
    builder.Logging.AddOpenTelemetry(logging =>
    {

        logging.SetResourceBuilder(resourceBuilder);

        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
        logging.ParseStateValues = true;

        logging.AddOtlpExporter();
        logging.AddConsoleExporter();
    });
}
else
{
    builder.Logging.AddJsonConsole(configure =>
    {
        //configure.IncludeScopes = true;
        configure.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
        {
            Indented = true,
        };
    });
}

var app = builder.Build();

app.MapHealthChecks("/healthz");

// Configure the HTTP request pipeline.

var apiGroup = app.MapGroup("/weatherforecast");

apiGroup.AddEndpointFilter<WeatherApiMetricsFilter>();

apiGroup.MapGet("/", WeatherForecastHandler.GetForecasts);

app.Run();

internal record WeatherForecast(
    DateOnly Date,
    int TemperatureC,
    string? Summary,
    [SensitiveData] string? SensitiveInformation)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

internal partial class WeatherForecastHandler
{
    static readonly string[] summaries = [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    internal static IEnumerable<WeatherForecast> GetForecasts(ILogger<WeatherForecastHandler> logger, WeatherApiMetrics weatherApiMetrics)
    {
        weatherApiMetrics.RecordRequest();

        var forecasts = new Bogus.Faker<WeatherForecast>()
            .CustomInstantiator(f =>
        {
            var forecast = new WeatherForecast(
                DateOnly.FromDateTime(f.Date.Future()),
                f.Random.Int(-20, 55),
                f.PickRandom(summaries),
                f.Finance.CreditCardNumber()
                );

            CreatedForecast(logger, forecast.Date, forecast);

            weatherApiMetrics.RecordTemperature(forecast);

            return forecast;
        })
        .Generate(5);

        return forecasts;
    }


    [LoggerMessage(LogLevel.Debug, Message = "Created weather forecast: {ForecastDate}")]
    static partial void CreatedForecast(
        ILogger logger,
        DateOnly forecastDate,
        [LogProperties] WeatherForecast forecast);
}

public readonly struct MyWebApiTaxonomy
{
    public static string TaxonomyName => typeof(MyWebApiTaxonomy).FullName!;

    public static DataClassification SensitiveData => new(TaxonomyName, nameof(SensitiveData));
}

public class SensitiveDataAttribute : DataClassificationAttribute
{
    public SensitiveDataAttribute() : base(MyWebApiTaxonomy.SensitiveData) { }
}

public class MaskingRedactor : Redactor
{

    const string mask = "***********";

    public override int GetRedactedLength(ReadOnlySpan<char> input)
        => mask.Length;

    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        mask.CopyTo(destination);

        destination[0] = source[0];

        return mask.Length;
    }
}