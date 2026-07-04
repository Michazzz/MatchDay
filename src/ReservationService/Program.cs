using System.Text.Json.Serialization;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using ReservationService.Consumers;
using ReservationService.Data;
using ReservationService.Endpoints;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<ReservationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("ReservationDb")
        ?? throw new InvalidOperationException("Connection string 'ReservationDb' is not configured.")));

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SeatsReservedConsumer>();
    x.AddConsumer<SeatsReservationRejectedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(
            builder.Configuration["RabbitMq:Host"] ?? "localhost",
            h =>
            {
                h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
            });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("ReservationService API"));
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReservationDbContext>();
    await db.Database.MigrateAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ReservationService" }))
   .WithName("HealthCheck");

app.MapReservationEndpoints();

app.Run();
