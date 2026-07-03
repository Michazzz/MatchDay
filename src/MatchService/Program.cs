var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Liveness probe — expanded into real match endpoints in the next step.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "MatchService" }))
   .WithName("HealthCheck");

app.Run();
