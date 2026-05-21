using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var toolName = "date-info"; // uppfyller kravet: innehåller "date"

// MCP endpoint
app.MapPost("/mcp", async (HttpContext ctx) =>
{
  using var reader = new StreamReader(ctx.Request.Body);
  var body = await reader.ReadToEndAsync();

  var json = JsonNode.Parse(body);
  var method = json?["method"]?.ToString();
  var id = json?["id"]?.ToString();

  JsonNode result = method switch
  {
    "initialize" => new JsonObject
    {
      ["protocolVersion"] = "2024-11-05",
      ["serverInfo"] = new JsonObject
      {
        ["name"] = "csharp-mcp",
        ["version"] = "1.0.0"
      }
    },

    "tools/list" => new JsonObject
    {
      ["tools"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = toolName,
                    ["description"] = "Returns weekday and ordinal occurrence for a given date",
                    ["inputSchema"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["date"] = new JsonObject
                            {
                                ["type"] = "string",
                                ["description"] = "ISO date (YYYY-MM-DD)"
                            }
                        },
                        ["required"] = new JsonArray { "date" }
                    }
                }
            }
    },

    "tools/call" => HandleToolCall(json),

    _ => new JsonObject
    {
      ["error"] = new JsonObject
      {
        ["code"] = -32601,
        ["message"] = $"Unknown method: {method}"
      }
    }
  };

  var response = new JsonObject
  {
    ["jsonrpc"] = "2.0",
    ["id"] = id,
    ["result"] = result
  };

  ctx.Response.ContentType = "application/json";
  await ctx.Response.WriteAsync(response.ToJsonString());
});

app.Run();

// ----------------------
// Tool handler
// ----------------------
JsonNode HandleToolCall(JsonNode? request)
{
  var tool = request?["params"]?["name"]?.ToString();
  var args = request?["params"]?["arguments"] as JsonObject;

  if (tool != "date-info")
    return new JsonObject
    {
      ["error"] = new JsonObject
      {
        ["code"] = -32601,
        ["message"] = $"Unknown tool: {tool}"
      }
    };

  var dateString = args?["date"]?.ToString();

  if (!DateTime.TryParse(dateString, out var date))
  {
    return new JsonObject
    {
      ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = $"Invalid date: {dateString}"
                }
            }
    };
  }

  var weekday = date.ToString("dddd", CultureInfo.InvariantCulture);
  var ordinal = GetOrdinalOccurrence(date);

  return new JsonObject
  {
    ["content"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "text",
                ["text"] = $"{weekday}, {ordinal} {weekday}"
            }
        }
  };
}

// ----------------------
// Ordinal logic
// ----------------------
string GetOrdinalOccurrence(DateTime date)
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
