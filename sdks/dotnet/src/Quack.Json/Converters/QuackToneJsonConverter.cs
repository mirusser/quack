using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quack;

internal sealed class QuackToneJsonConverter : JsonConverter<QuackTone>
{
    public override QuackTone Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString()?.ToLowerInvariant();
        return value switch
        {
            "serious-duck" => QuackTone.SeriousDuck,
            "playful-duck" => QuackTone.PlayfulDuck,
            "angry-goose" => QuackTone.AngryGoose,
            "sleepy-duckling" => QuackTone.SleepyDuckling,
            _ => throw new JsonException($"Unknown tone: {value}"),
        };
    }

    public override void Write(Utf8JsonWriter writer, QuackTone value, JsonSerializerOptions options)
    {
        var str = value switch
        {
            QuackTone.SeriousDuck => "serious-duck",
            QuackTone.PlayfulDuck => "playful-duck",
            QuackTone.AngryGoose => "angry-goose",
            QuackTone.SleepyDuckling => "sleepy-duckling",
            _ => throw new JsonException($"Unknown tone: {value}"),
        };
        writer.WriteStringValue(str);
    }
}
