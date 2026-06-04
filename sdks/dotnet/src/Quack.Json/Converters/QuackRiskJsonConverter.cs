using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quack;

internal sealed class QuackRiskJsonConverter : JsonConverter<QuackRisk>
{
    public override QuackRisk Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString()?.ToLowerInvariant();
        return value switch
        {
            "n" or "none" => QuackRisk.None,
            "l" or "low" => QuackRisk.Low,
            "m" or "med" or "medium" => QuackRisk.Medium,
            "h" or "high" => QuackRisk.High,
            "c" or "crit" or "critical" => QuackRisk.Critical,
            _ => throw new JsonException($"Unknown risk value: {value}"),
        };
    }

    public override void Write(Utf8JsonWriter writer, QuackRisk value, JsonSerializerOptions options)
    {
        var str = value switch
        {
            QuackRisk.None => "n",
            QuackRisk.Low => "l",
            QuackRisk.Medium => "m",
            QuackRisk.High => "h",
            QuackRisk.Critical => "c",
            _ => throw new JsonException($"Unknown risk: {value}"),
        };
        writer.WriteStringValue(str);
    }
}
