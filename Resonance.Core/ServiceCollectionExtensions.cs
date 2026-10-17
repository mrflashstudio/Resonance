using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Resonance.Core.Commands;
using Resonance.Core.Configuration;
using Resonance.Core.Models;
using Resonance.Core.Services;
using Resonance.Core.Validation;

namespace Resonance.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResonanceCore(this IServiceCollection services)
    {
        services.Configure<PasswordHasherOptions>(options => options.IterationCount = 210_000);
        services.AddSingleton<IPasswordHasher<Account>, PasswordHasher<Account>>();
        services.AddSingleton<IValidator<RegistrationInput>, RegistrationValidator>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<ChatService>();
        services.AddSingleton<ModerationService>();
        services.AddSingleton<BotCommandDispatcher>();
        services.AddSingleton<ServerBotService>();
        services.AddOptions<ServerBotSettings>()
            .BindConfiguration("ServerBot")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Name) && options.Name.Length is >= 3 and <= 32 &&
                options.Name == options.Name.Trim() && !options.Name.Any(char.IsControl),
                "ServerBot:Name must be between 3 and 32 characters without control characters or leading/trailing whitespace.")
            .ValidateOnStart();
        services.AddOptions<SessionSettings>()
            .BindConfiguration("Sessions")
            .Validate(options => options.Lifetime > SessionSettings.RenewalInterval && options.Lifetime <= TimeSpan.FromDays(30),
                "Sessions:Lifetime must be greater than one day and at most 30 days.")
            .Validate(options => options.CleanupInterval > TimeSpan.Zero && options.CleanupInterval <= TimeSpan.FromDays(1),
                "Sessions:CleanupInterval must be greater than zero and at most one day.")
            .ValidateOnStart();
        return services;
    }
}
