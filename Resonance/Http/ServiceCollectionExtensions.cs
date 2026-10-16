using Carter;
using Resonance.Configuration;
using Resonance.Http.Validation;

namespace Resonance.Http;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResonanceHttp(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ServerOptions>()
            .BindConfiguration("Server")
            .Validate(options => Uri.CheckHostName(options.Domain) == UriHostNameType.Dns &&
                options.Domain.Contains('.') && !options.Domain.EndsWith('.'),
                "Server:Domain must be a domain without a scheme, port, or trailing dot.")
            .ValidateOnStart();
        services.AddCarter();
        services.AddRequestValidation();
        services.AddRateLimiting(configuration);
        services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            var domain = configuration["Server:Domain"];
            policy.WithOrigins($"https://{domain}", $"https://www.{domain}")
                .WithMethods("GET", "POST").WithHeaders("Content-Type").AllowCredentials();
        }));
        return services;
    }
}
