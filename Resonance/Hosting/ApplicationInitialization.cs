using Microsoft.Extensions.Options;
using Resonance.Core.Configuration;
using Resonance.Core.Services;
using Resonance.Server.Services;
using Resonance.Server.Sockets.Handlers;

namespace Resonance.Hosting;

public static class ApplicationInitialization
{
    public static async Task InitializeServerAsync(this WebApplication app)
    {
        app.Services.GetRequiredService<IStartupValidator>().Validate();
        _ = app.Services.GetRequiredService<IPacketDispatcher>();
        var botSettings = app.Services.GetRequiredService<IOptions<ServerBotSettings>>().Value;
        var bot = await app.Services.GetRequiredService<ServerBotService>()
            .InitializeAsync(botSettings.Name, app.Lifetime.ApplicationStopping);
        if (bot is not null)
            app.Services.GetRequiredService<OnlinePlayers>().SetServerBot(bot);
    }
}
