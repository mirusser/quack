using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quack;

/// <summary>
/// Quack-JSON codec. Serializes and deserializes Quack frames to/from JSON
/// using camelCase wire names per the protocol spec.
/// </summary>
public static class QuackJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new QuackVerbJsonConverter(),
            new QuackRiskJsonConverter(),
            new QuackToneJsonConverter(),
        },
    };

    /// <summary>Serialize a frame to a JSON string.</summary>
    public static string Serialize(QuackFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        return JsonSerializer.Serialize(frame, Options);
    }

    /// <summary>Deserialize a JSON string to a frame.</summary>
    public static QuackFrame Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<QuackFrame>(json, Options)
            ?? throw new JsonException("Failed to deserialize QuackFrame");
    }

    /// <summary>Serialize a frame to a JSON stream.</summary>
    public static async ValueTask SerializeAsync(Stream stream, QuackFrame frame, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(frame);
        await JsonSerializer.SerializeAsync(stream, frame, Options, ct).ConfigureAwait(false);
    }

    /// <summary>Deserialize a JSON stream to a frame.</summary>
    public static async ValueTask<QuackFrame> DeserializeAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var result = await JsonSerializer.DeserializeAsync<QuackFrame>(stream, Options, ct).ConfigureAwait(false);
        return result ?? throw new JsonException("Failed to deserialize QuackFrame from stream");
    }
}
