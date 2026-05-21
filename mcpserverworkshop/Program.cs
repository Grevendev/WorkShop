var builder = WebApplication.CreateBuilder(args);

// Minimal API + MVC
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers(); // aktiverar MVC‑routing

// Minimal API routes
app.MapGet("/api/hello", () => "Hello from Minimal API");

app.Run();