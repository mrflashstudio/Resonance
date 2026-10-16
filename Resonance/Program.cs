using Carter;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Resonance.Configuration;
using Resonance.Core;
using Resonance.Database;
using Resonance.Hosting;
using Resonance.Http;
using Resonance.Server;

Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
DeploymentConfiguration.Apply(builder.Configuration);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddResonanceCore();
builder.Services.AddResonanceDatabase(builder.Configuration);
builder.Services.AddResonanceServer();
builder.Services.AddResonanceHttp(builder.Configuration);
builder.Services.AddHostedService<SessionCleanupService>();

var app = builder.Build();

if (args.Contains("--migrate", StringComparer.Ordinal))
{
    await using var db = await app.Services.GetRequiredService<IDbContextFactory<ResonanceDbContext>>()
        .CreateDbContextAsync();
    await db.Database.MigrateAsync();
    return;
}

await app.InitializeServerAsync();

app.UseWebSockets();
app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.MapCarter();

await app.RunAsync();
