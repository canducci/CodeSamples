using BlazorPwaIdentity.Server.Data;

namespace BlazorPwaIdentity.Server;

public class SeedData
{
    public static void Execute(ApplicationDbContext db)
    {
        db.Forecast.AddRange(CreateRandom());
        db.SaveChanges();
    }

    private static IEnumerable<Weather> CreateRandom()
    {

        var summaries = new[] { "Sunny", "Cloudy", "Rainy" };

        for (int i = 0; i < 10; i++)
        {
            var date = DateOnly.FromDateTime(DateTime.Today.AddDays(i));
            var temp = Random.Shared.Next(0, 35);
            var summary = summaries[Random.Shared.Next(0, 3)];

            yield return new Weather
            {
                Date = date,
                Summary = summary,
                TemperatureC = temp
            };
        }
    }


}
