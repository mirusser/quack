namespace Quack.Conformance;

using System.Net.Http;
using System.Text;
using System.Text.Json;

sealed class ConformanceClient(HttpClient http, string endpoint)
{
    static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    const string ConformPath = "/quack/conform";

    public async Task<ConformResponse> SendAsync(ConformRequest request, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(request, Options);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await http.PostAsync(endpoint + ConformPath, content, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new ConformanceNetworkException($"Network error: {ex.Message}", ex);
        }

        var body = await response.Content.ReadAsStringAsync(ct);

        try
        {
            return JsonSerializer.Deserialize<ConformResponse>(body, Options)
                ?? throw new InvalidOperationException("null body");
        }
        catch (JsonException ex)
        {
            throw new ConformanceProtocolException(
                $"Agent returned non-JSON body (HTTP {(int)response.StatusCode}): {body[..Math.Min(200, body.Length)]}", ex);
        }
    }
}

sealed class ConformanceNetworkException(string message, Exception inner)
    : Exception(message, inner);

sealed class ConformanceProtocolException(string message, Exception inner)
    : Exception(message, inner);
