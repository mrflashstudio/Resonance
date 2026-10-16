using FluentValidation;
using Resonance.Models;

namespace Resonance.Http.Validation;

public static class ValidationRegistration
{
    public static IServiceCollection AddRequestValidation(this IServiceCollection services)
    {
        services.AddSingleton<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddSingleton<IValidator<LogoutRequest>, LogoutRequestValidator>();
        services.AddSingleton<IValidator<ProfileRequest>, ProfileRequestValidator>();
        return services;
    }
}
