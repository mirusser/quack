using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quack;

internal sealed class QuackVerbJsonConverter : JsonConverter<QuackVerb>
{
    public override QuackVerb Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var verb = reader.GetString();
        return verb?.ToLowerInvariant() switch
        {
            "quack" => QuackVerb.Quack,
            "peck" => QuackVerb.Peck,
            "bob" => QuackVerb.Bob,
            "nack" => QuackVerb.Nack,
            "egg" => QuackVerb.Egg,
            "hatch" => QuackVerb.Hatch,
            "flap" => QuackVerb.Flap,
            "perch" => QuackVerb.Perch,
            "honk" => QuackVerb.Honk,
            "molt" => QuackVerb.Molt,
            "splash" => QuackVerb.Splash,
            _ => throw new JsonException($"Unknown verb: {verb}"),
        };
    }

    public override void Write(Utf8JsonWriter writer, QuackVerb value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToLowerInvariant());
    }
}
