using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Resonance.Authentication;
using Resonance.Configuration;
using Resonance.Data;
using Resonance.Data.Entities;
using Resonance.Routing;
using Resonance.Services;
using Resonance.Sockets;
using Resonance.Sockets.Handlers;

Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
DeploymentConfiguration.Apply(builder.Configuration);
var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException("ConnectionStrings:Database must be configured.");

builder.Services.AddDbContextFactory<ResonanceDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.Configure<PasswordHasherOptions>(options => options.IterationCount = 210_000);
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOptions<GameConnectionSettings>()
    .BindConfiguration("GameConnection")
    .Validate(options => options.InactivityTimeout > TimeSpan.Zero && options.InactivityTimeout <= TimeSpan.FromHours(1),
        "GameConnection:InactivityTimeout must be greater than zero and at most one hour.")
    .ValidateOnStart();
builder.Services.AddOptions<SessionSettings>()
    .BindConfiguration("Sessions")
    .Validate(options => options.Lifetime > TimeSpan.Zero && options.Lifetime <= TimeSpan.FromDays(30),
        "Sessions:Lifetime must be greater than zero and at most 30 days.")
    .Validate(options => options.CleanupInterval > TimeSpan.Zero && options.CleanupInterval <= TimeSpan.FromDays(1),
        "Sessions:CleanupInterval must be greater than zero and at most one day.")
    .ValidateOnStart();
builder.Services.AddOptions<ServerOptions>()
    .BindConfiguration("Server")
    .Validate(options => Uri.CheckHostName(options.Domain) == UriHostNameType.Dns &&
        options.Domain.Contains('.') && !options.Domain.EndsWith('.'),
        "Server:Domain must be a domain without a scheme, port, or trailing dot.")
    .ValidateOnStart();

builder.Services.AddControllers();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    var domain = builder.Configuration["Server:Domain"];
    policy.WithOrigins($"https://{domain}", $"https://www.{domain}")
        .WithMethods("GET", "POST").WithHeaders("Content-Type").AllowCredentials();
}));
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
builder.Services.AddHostedService<SessionCleanupService>();
builder.Services.AddSingleton<IOnlinePlayers, OnlinePlayers>();
builder.Services.AddSingleton<UserActivityService>();
builder.Services.AddSingleton<IGameConnectionService, GameConnectionService>();
builder.Services.AddSingleton<IPacketDispatcher, PacketDispatcher>();
builder.Services.AddSingleton<IPacketHandler, ProfileHandler>();
builder.Services.AddSingleton<IPacketHandler, OnlinePlayersHandler>();
builder.Services.AddSingleton<IPacketHandler, PingHandler>();

var app = builder.Build();

if (args.Contains("--migrate", StringComparer.Ordinal))
{
    await using var db = await app.Services.GetRequiredService<IDbContextFactory<ResonanceDbContext>>()
        .CreateDbContextAsync();
    await db.Database.MigrateAsync();
    return;
}

app.UseWebSockets();
app.UseRouting();
app.UseCors();
app.MapControllers();

await app.RunAsync();
