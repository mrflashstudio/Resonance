using Microsoft.Extensions.Configuration.Memory;
using Npgsql;

namespace Resonance.Configuration;

public static class DeploymentConfiguration
{
    public static void Apply(ConfigurationManager configuration)
    {
        var port = configuration.GetValue("RESONANCE_PORT", 3333);

        if (port is < 1 or > 65535)
            throw new InvalidOperationException("RESONANCE_PORT must be between 1 and 65535.");

        configuration["HTTP_PORTS"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var defaults = new Dictionary<string, string?>
        {
            ["Server:Domain"] = configuration["Server:Domain"] ?? Required(configuration, "RESONANCE_DOMAIN")
        };

        var configuredConnection = configuration.GetConnectionString("Database");

        if (configuredConnection is not null && string.IsNullOrWhiteSpace(configuredConnection))
            throw new InvalidOperationException("ConnectionStrings:Database must not be empty.");

        if (configuredConnection is null)
        {
            var connection = new NpgsqlConnectionStringBuilder
            {
                Host = configuration["POSTGRES_HOST"] ?? "localhost",
                Port = configuration.GetValue("POSTGRES_PORT", 5432),
                Database = Required(configuration, "POSTGRES_DB"),
                Username = Required(configuration, "POSTGRES_USER"),
                Password = Required(configuration, "POSTGRES_PASSWORD")
            };

            defaults["ConnectionStrings:Database"] = connection.ConnectionString;
        }

        configuration.Sources.Insert(0, new MemoryConfigurationSource { InitialData = defaults });
    }

    private static string Required(IConfiguration configuration, string key)
    {
        var value = configuration[key];

        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"{key} must be configured in .env or the environment.");
    }
}
