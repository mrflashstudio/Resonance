using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Resonance.Core.Services;
using Resonance.Core.Configuration;
using Resonance.Http.Filters;
using Resonance.Models;

namespace Resonance.Http;

public static class RateLimiting
{
    public const string Authentication = "authentication";

    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("RateLimits").Get<RateLimitSettings>() ?? new();

        if (settings.AuthenticationConcurrency <= 0)
            throw new InvalidOperationException("RateLimits:AuthenticationConcurrency must be greater than zero.");

        var limiter = new AccountActionLimiter(settings);
        services.AddSingleton(_ => limiter);

        return services.AddRateLimiter(options =>
        {
            options.AddConcurrencyLimiter(Authentication, limiter =>
            {
                limiter.PermitLimit = settings.AuthenticationConcurrency;
                limiter.QueueLimit = 0;
            });
            options.OnRejected = async (rejected, cancellationToken) =>
            {
                var context = rejected.HttpContext;
                SetRejectionHeaders(context, TimeSpan.FromSeconds(1));

                if (context.Request.Path == "/api/loginAccount")
                    await context.Response.WriteAsJsonAsync(new LoginResponse("Too many requests. Please try again later."),
                        cancellationToken);
                else
                    await TypedResults.Problem(statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Too many requests. Please try again later.").ExecuteAsync(context);
            };
        });
    }

    public static void SetRejectionHeaders(HttpContext context, TimeSpan retryAfter)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
            .ToString(CultureInfo.InvariantCulture);
        EndpointFilters.DisableCaching(context.Response);
    }
}
