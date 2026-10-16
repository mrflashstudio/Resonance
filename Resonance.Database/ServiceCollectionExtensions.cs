using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Resonance.Core.Repositories;
using Resonance.Database.Repositories;

namespace Resonance.Database;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResonanceDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("ConnectionStrings:Database must be configured.");
        services.AddDbContextFactory<ResonanceDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<ISessionRepository, SessionRepository>();
        return services;
    }
}
