using System.Text.Json;

namespace Resonance.Http;

internal static class GameRequestReader
{
    public static async Task<T?> ReadAsync<T>(HttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<T>(request.Body, JsonSerializerOptions.Web, cancellationToken);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
