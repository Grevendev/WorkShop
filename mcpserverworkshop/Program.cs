using System.Text.Json;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ---------------------------------------------------------
// MCP ENDPOINT (ENDA ENDPOINTEN SOM MCP-KLIENTER ANVÄNDER)
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

        // IMPORTANT: DeepClone() fixes "node already has a parent"
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
// TOOL 2: weather-forecast (mockad version)
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

    // Mockat svar – funkar för verifiering
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
app.Run();
