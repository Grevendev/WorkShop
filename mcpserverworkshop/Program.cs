using Microsoft.AspNetCore.Mvc;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/mcp", async ([FromBody] McpRequest request) =>
{
  if (request.Method == "tools.call" && request.Params?.Tool == "weekday_date")
  {
    var dateString = request.Params.Arguments["date"]?.ToString();
    if (!DateTime.TryParse(dateString, out var date))
    {
      return Results.Json(new
      {
        content = new[]
          {
                    new { type = "text", text = $"Invalid date: {dateString}" }
                }
      });
    }

    var weekday = date.ToString("dddd", CultureInfo.InvariantCulture);
    var ordinal = GetOrdinalOccurrence(date);

    return Results.Json(new
    {
      content = new[]
        {
                new { type = "text", text = $"{weekday}, {ordinal} {weekday}" }
            }
    });
  }

  return Results.Json(new
  {
    content = new[]
      {
            new { type = "text", text = "Unknown tool or method" }
        }
  });
});

app.Run();

static string GetOrdinalOccurrence(DateTime date)
{
  int count = 0;
  for (int day = 1; day <= date.Day; day++)
  {
    var d = new DateTime(date.Year, date.Month, day);
    if (d.DayOfWeek == date.DayOfWeek)
      count++;
  }

  return count switch
  {
    1 => "1st",
    2 => "2nd",
    3 => "3rd",
    _ => $"{count}th"
  };
}

public class McpRequest
{
  public string Method { get; set; }
  public McpParams Params { get; set; }
}

public class McpParams
{
  public string Tool { get; set; }
  public Dictionary<string, object> Arguments { get; set; }
}
