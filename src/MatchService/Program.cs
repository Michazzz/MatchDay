using System.Text.Json.Serialization;
using MatchService.Data;
using MatchService.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<MatchDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("MatchDb")
        ?? throw new InvalidOperationException("Connection string 'MatchDb' is not configured.")));

// Serialize enums (e.g. MatchStatus) as their names rather than integers.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Apply migrations and seed the catalog on startup (demo convenience).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MatchDbContext>();
    await db.Database.MigrateAsync();
    await MatchSeeder.SeedAsync(db);
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "MatchService" }))
   .WithName("HealthCheck");

app.MapMatchEndpoints();
app.MapReferenceEndpoints();

app.Run();
