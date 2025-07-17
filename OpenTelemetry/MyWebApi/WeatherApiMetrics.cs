using Microsoft.Extensions.Diagnostics.Metrics;
using System.Diagnostics.Metrics;

namespace MyWebApi;

public class WeatherApiMetrics
{
    public const string MeterName = "MyWebApi.WeatherApi";
    private readonly Histogram<int> weather_api_forecast_temperature;
    private readonly Counter<int> weather_api_requests;

    public WeatherApiMetrics(IMeterFactory meterFactory)
    {

        var meter = meterFactory.Create(MeterName);

        weather_api_forecast_temperature = meter.CreateHistogram<int>(
            "weather.api.forecast.temperature", "C", "Temperature (C) returned");
        weather_api_requests = meter.CreateCounter<int>("weather.api.requests",
            description: "Total number of requests to the Weather API");
    }
    public void RecordRequest()
    {
        weather_api_requests.Add(1);
    }

    internal void RecordTemperature(WeatherForecast forecast)
    {
        weather_api_forecast_temperature.Record(forecast.TemperatureC, new KeyValuePair<string, object?>("Weekday", forecast.Date.DayOfWeek.ToString()));
    }
}


public class WeatherApiMetricsFilter : IEndpointFilter
{
    private readonly WeatherApiMetrics _metrics;
    public WeatherApiMetricsFilter(WeatherApiMetrics metrics)
    {
        _metrics = metrics;
    }
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        _metrics.RecordRequest();
        // Call the next middleware in the pipeline
        return await next(context);
    }
}