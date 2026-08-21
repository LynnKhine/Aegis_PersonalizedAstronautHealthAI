using Aegis.Api.Hubs;
using Aegis.Api.Services;
using Aegis.Core.Interfaces;
using Aegis.Core.Services;
using Aegis.Infrastructure.Data;
using Aegis.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Infrastructure (EF Core, repositories, WatsonxClient) ───────────────────
var connectionString = builder.Configuration.GetConnectionString("AegisDb")
    ?? "Data Source=aegis.db";

builder.Services.AddAegisInfrastructure(connectionString, builder.Configuration);

// ─── Domain services ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IDeviationScoringService, DeviationScoringService>();

// ─── API + SignalR ────────────────────────────────────────────────────────────
builder.Services.AddHostedService<LiveSimulationWorker>();
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Serialize enums as strings so the dashboard JS can match by name
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Aegis Health API", Version = "v1" });
});

// Allow the dashboard to talk to the API when opened from a file or different port
builder.Services.AddCors(opts => opts.AddDefaultPolicy(p =>
    p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

// ─── Auto-migrate on startup ──────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
    await db.Database.MigrateAsync();
}

// ─── Middleware pipeline ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseDefaultFiles();   // serves wwwroot/index.html on /
app.UseStaticFiles();    // serves wwwroot/**
app.UseHttpsRedirection();
app.MapControllers();
app.MapHub<AegisHub>("/hubs/aegis");

app.Run();
