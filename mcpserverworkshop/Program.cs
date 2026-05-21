using System.Text.Json;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// MCP-tool definition
var toolName = "greet-day"; // uppfyller regeln

if (!toolName.Contains("day") && !toolName.Contains("week") && !toolName.Contains("date"))
  throw new Exception("Tool name must contain 'day', 'week', or 'date'");

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
                    ["description"] = "Returns a greeting for the given name",
                    ["inputSchema"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["name"] = new JsonObject
                            {
                                ["type"] = "string",
                                ["description"] = "The name to greet"
                            }
                        },
                        ["required"] = new JsonArray { "name" }
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

  if (tool != "greet-day")
    return new JsonObject
    {
      ["error"] = new JsonObject
      {
        ["code"] = -32601,
        ["message"] = $"Unknown tool: {tool}"
      }
    };

  var name = args?["name"]?.ToString() ?? "unknown";

  return new JsonObject
  {
    ["content"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "text",
                ["text"] = $"Hello {name}"
            }
        }
  };
}
