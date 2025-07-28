using BlazorPwaIdentity.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorPwaIdentity.Server.Api;

public static class ForecastEndpointsExtensions
{
    extension(IEndpointRouteBuilder builder)
    {
        public IEndpointConventionBuilder MapForecastEndpoints()
        {
            var apiGroup = builder.MapGroup("/api/forecast")
                .RequireAuthorization();


            apiGroup.MapGet("/", async (ApplicationDbContext dbContext, CancellationToken token) =>
            {
                return await dbContext.Forecast.AsNoTracking().ToArrayAsync(token);
            }).Produces<IEnumerable<Weather>>();

            return apiGroup;
        }
    }
}

