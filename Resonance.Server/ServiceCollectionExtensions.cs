using Microsoft.Extensions.DependencyInjection;
using Resonance.Core.Services;
using Resonance.Server.Configuration;
using Resonance.Server.Services;
using Resonance.Server.Sockets;
using Resonance.Server.Sockets.Handlers;

namespace Resonance.Server;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResonanceServer(this IServiceCollection services)
    {
        services.AddOptions<GameConnectionSettings>()
            .BindConfiguration("GameConnection")
            .Validate(options => options.InactivityTimeout > TimeSpan.Zero && options.InactivityTimeout <= TimeSpan.FromHours(1),
                "GameConnection:InactivityTimeout must be greater than zero and at most one hour.")
            .ValidateOnStart();
        services.AddSingleton<ChatChannels>();
        services.AddSingleton<OnlinePlayers>();
        services.AddSingleton<IOnlinePlayers>(services => services.GetRequiredService<OnlinePlayers>());
        services.AddSingleton<IPresence>(services => services.GetRequiredService<IOnlinePlayers>());
        services.AddSingleton<UserActivityService>();
        services.AddSingleton<IGameConnectionService, GameConnectionService>();
        services.AddSingleton<IPacketDispatcher, PacketDispatcher>();
        return services;
    }
}
