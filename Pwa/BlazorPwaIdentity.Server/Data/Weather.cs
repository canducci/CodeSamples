using System.ComponentModel.DataAnnotations;

namespace BlazorPwaIdentity.Server.Data;

public class Weather
{
    [Key]
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
}
