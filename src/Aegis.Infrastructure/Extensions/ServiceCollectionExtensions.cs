using Aegis.Core.Interfaces;
using Aegis.Infrastructure.Data;
using Aegis.Infrastructure.Repositories;
using Aegis.Infrastructure.Watsonx;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers EF Core (SQLite), all repositories, and the WatsonxClient.
    /// Call this from both Aegis.Api and Aegis.Simulation Program.cs.
    /// </summary>
    public static IServiceCollection AddAegisInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        // EF Core — SQLite
        services.AddDbContext<AegisDbContext>(opts =>
            opts.UseSqlite(connectionString));

        // Repositories
        services.AddScoped<IAstronautRepository, AstronautRepository>();
        services.AddScoped<IBiometricReadingRepository, BiometricReadingRepository>();
        services.AddScoped<IPersonalBaselineRepository, PersonalBaselineRepository>();
        services.AddScoped<IInterventionPlanRepository, InterventionPlanRepository>();

        // watsonx.ai
        services.Configure<WatsonxOptions>(configuration.GetSection(WatsonxOptions.SectionName));
        services.AddHttpClient("watsonx");
        services.AddScoped<IWatsonxClient, WatsonxClient>();

        return services;
    }
}
