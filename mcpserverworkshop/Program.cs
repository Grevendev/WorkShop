using System.Text.Json;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ---------------------------------------------------------
// GLOBAL STATE (CACHE + HTTP CLIENT)
// ---------------------------------------------------------
List<(string Id, string Title, string Content)> KnowledgeBase = new();
HttpClient http = new HttpClient();

// ---------------------------------------------------------
// MCP ENDPOINT
// ---------------------------------------------------------
app.MapPost("/mcp", async (HttpContext ctx) =>
{
  try
  {
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();

    var json = JsonNode.Parse(body);
    var method = json?["method"]?.ToString();
    var id = json?["id"];

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

      "tools/list" => ListTools(),

      "tools/call" => await HandleToolCall(json),

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
      ["id"] = id?.DeepClone(),
      ["result"] = result.DeepClone()
    };

    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsync(response.ToJsonString());
  }
  catch (Exception ex)
  {
    Console.WriteLine("MCP ERROR: " + ex);

    var errorResponse = new JsonObject
    {
      ["jsonrpc"] = "2.0",
      ["id"] = null,
      ["error"] = new JsonObject
      {
        ["code"] = -32000,
        ["message"] = ex.Message
      }
    };

    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsync(errorResponse.ToJsonString());
  }
});

// ---------------------------------------------------------
// TOOLS/LIST
// ---------------------------------------------------------
JsonNode ListTools()
{
  return new JsonObject
  {
    ["tools"] = new JsonArray
        {
            // Tool 1: weekday-date
            new JsonObject
            {
                ["name"] = "weekday-date",
                ["description"] = "Returns weekday name and ordinal occurrence for a given ISO date",
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
            },

            // Tool 2: weather-forecast
            new JsonObject
            {
                ["name"] = "weather-forecast",
                ["description"] = "Fetches simple weather info for a given city and date",
                ["inputSchema"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["city"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "City name"
                        },
                        ["date"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "ISO date (YYYY-MM-DD)"
                        }
                    },
                    ["required"] = new JsonArray { "city", "date" }
                }
            },

            // Tool 3: search_documents (RAG)
            new JsonObject
            {
                ["name"] = "search_documents",
                ["description"] = "Searches the knowledge base for relevant documents",
                ["inputSchema"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["query"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "Free-text search query"
                        },
                        ["limit"] = new JsonObject
                        {
                            ["type"] = "number",
                            ["description"] = "Maximum number of results (default 5)"
                        }
                    },
                    ["required"] = new JsonArray { "query" }
                }
            }
        }
  };
}

// ---------------------------------------------------------
// TOOLS/CALL
// ---------------------------------------------------------
async Task<JsonNode> HandleToolCall(JsonNode? request)
{
  var tool = request?["params"]?["name"]?.ToString();
  var args = request?["params"]?["arguments"] as JsonObject ?? new JsonObject();

  return tool switch
  {
    "weekday-date" => HandleWeekdayDate(args),
    "weather-forecast" => await HandleWeatherForecast(args),
    "search_documents" => await HandleSearchDocuments(args),

    _ => new JsonObject
    {
      ["error"] = new JsonObject
      {
        ["code"] = -32601,
        ["message"] = $"Unknown tool: {tool}"
      }
    }
  };
}

// ---------------------------------------------------------
// TOOL 1: weekday-date
// ---------------------------------------------------------
JsonNode HandleWeekdayDate(JsonObject args)
{
  var iso = args?["date"]?.ToString();
  if (iso is null)
  {
    return new JsonObject
    {
      ["error"] = new JsonObject
      {
        ["code"] = -32602,
        ["message"] = "Missing required parameter: date"
      }
    };
  }

  var date = DateOnly.Parse(iso);
  var weekday = date.DayOfWeek.ToString();
  int ordinal = (date.Day - 1) / 7 + 1;

  string ordinalStr = ordinal switch
  {
    1 => "1st",
    2 => "2nd",
    3 => "3rd",
    _ => $"{ordinal}th"
  };

  return new JsonObject
  {
    ["content"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "text",
                ["text"] = $"{weekday}, {ordinalStr}"
            }
        }
  };
}

// ---------------------------------------------------------
// TOOL 2: weather-forecast (mockad)
// ---------------------------------------------------------
async Task<JsonNode> HandleWeatherForecast(JsonObject args)
{
  var city = args?["city"]?.ToString();
  var date = args?["date"]?.ToString();

  if (city is null || date is null)
  {
    return new JsonObject
    {
      ["error"] = new JsonObject
      {
        ["code"] = -32602,
        ["message"] = "Missing required parameters: city and date"
      }
    };
  }

  return new JsonObject
  {
    ["content"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "text",
                ["text"] = $"Weather in {city} on {date}: Sunny, max 21°C"
            }
        }
  };
}

// ---------------------------------------------------------
// TOOL 3: search_documents (RAG)
// ---------------------------------------------------------
async Task<JsonNode> HandleSearchDocuments(JsonObject args)
{
  var query = args?["query"]?.ToString();
  var limit = args?["limit"]?.GetValue<int?>() ?? 5;

  if (string.IsNullOrWhiteSpace(query))
  {
    return new JsonObject
    {
      ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = "Query is required"
                }
            }
    };
  }

  // Lazy-load knowledge base
  if (KnowledgeBase.Count == 0)
  {
    var json = await http.GetStringAsync("https://tribetrot.ngrok.app/api/knowledge-base");
    var root = JsonNode.Parse(json);
    var docs = root?["documents"]?.AsArray() ?? new JsonArray();

    foreach (var d in docs)
    {
      var id = d?["id"]?.ToString() ?? "";
      var title = d?["title"]?.ToString() ?? "";
      var content = d?["content"]?.ToString() ?? "";

      if (!string.IsNullOrWhiteSpace(id))
        KnowledgeBase.Add((id, title, content));
    }
  }

  var words = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

  var scored = KnowledgeBase
      .Select(doc =>
      {
        var text = (doc.Title + " " + doc.Content).ToLower();
        int score = words.Count(w => text.Contains(w));
        return new { doc.Id, doc.Title, doc.Content, score };
      })
      .Where(x => x.score > 0)
      .OrderByDescending(x => x.score)
      .Take(limit)
      .ToList();

  var arr = new JsonArray(
      scored.Select(x =>
      {
        var o = new JsonObject
        {
          ["id"] = x.Id,
          ["title"] = x.Title,
          ["content"] = x.Content,
          ["score"] = x.score
        };
        return o;
      }).ToArray()
  );

  return new JsonObject
  {
    ["content"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "text",
                ["text"] = arr.ToJsonString()
            }
        }
  };
}

// ---------------------------------------------------------
app.Run();
