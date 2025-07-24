using Microsoft.Extensions.Diagnostics.Metrics;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MyWebApi;

public class WeatherApiDiag
{
    public const string MeterName = "MyWebApi.WeatherApi";
    private static readonly Histogram<int> weather_api_forecast_temperature;
    private static readonly Counter<int> weather_api_requests;
    public static readonly ActivitySource activitySource;

    static WeatherApiDiag()
    {
        activitySource = new ActivitySource(MeterName) ;

        var meter = new Meter(MeterName);

        weather_api_forecast_temperature = meter.CreateHistogram<int>(
            "weather.api.forecast.temperature", "C", "Temperature (C) returned");
        weather_api_requests = meter.CreateCounter<int>("weather.api.requests",
            description: "Total number of requests to the Weather API");
    }
    public static void RecordRequest()
    {
        weather_api_requests.Add(1);
    }

    internal static void RecordTemperature(WeatherForecast forecast)
    {
        weather_api_forecast_temperature.Record(forecast.TemperatureC, new KeyValuePair<string, object?>("Weekday", forecast.Date.DayOfWeek.ToString()));
    }
}


public class WeatherApiMetricsFilter : IEndpointFilter
{

    public WeatherApiMetricsFilter()
    {

    }
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        WeatherApiDiag.RecordRequest();
        // Call the next middleware in the pipeline
        return await next(context);
    }
}