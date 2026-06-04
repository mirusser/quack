using System.Formats.Cbor;

namespace Quack;

/// <summary>
/// Quack-CBOR codec. Deterministic CBOR encode/decode using integer keys
/// per the protocol spec §5.3.
/// </summary>
public static class QuackCbor
{
    // Integer key mapping per spec §5.3
    private const int KeyVersion = 1;
    private const int KeyVerb = 2;
    private const int KeyId = 3;
    private const int KeyTimestamp = 4;
    private const int KeySource = 5;
    private const int KeyDestination = 6;
    private const int KeyContext = 7;
    private const int KeyCorrelation = 8;
    private const int KeyRisk = 9;
    private const int KeyDigest = 10;
    private const int KeySummary = 11;
    private const int KeyData = 12;
    private const int KeyTtl = 13;
    private const int KeyTone = 14;
    // Profile-extension keys (15+)
    private const int KeyProfile = 15;
    private const int KeyExpiresAt = 16;
    private const int KeyTaskId = 17;

    /// <summary>Encode a frame to deterministic CBOR bytes.</summary>
    public static byte[] Encode(QuackFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var writer = new CborWriter(CborConformanceMode.Lax);
        writer.WriteStartMap(null);

        writer.WriteInt32(KeyVersion);
        writer.WriteTextString(frame.Version);

        writer.WriteInt32(KeyVerb);
        writer.WriteTextString(frame.Verb.ToString().ToLowerInvariant());

        if (!string.IsNullOrEmpty(frame.Id))
        {
            writer.WriteInt32(KeyId);
            writer.WriteTextString(frame.Id);
        }

        if (frame.Timestamp is { } ts)
        {
            writer.WriteInt32(KeyTimestamp);
            writer.WriteTextString(ts);
        }

        writer.WriteInt32(KeySource);
        writer.WriteTextString(frame.Source);

        if (frame.Destination is { } dst)
        {
            writer.WriteInt32(KeyDestination);
            writer.WriteTextString(dst);
        }

        if (frame.Context is { } ctx)
        {
            writer.WriteInt32(KeyContext);
            writer.WriteTextString(ctx);
        }

        if (frame.Correlation is { } corr)
        {
            writer.WriteInt32(KeyCorrelation);
            writer.WriteTextString(corr);
        }

        writer.WriteInt32(KeyRisk);
        writer.WriteTextString(SerializeRisk(frame.Risk));

        if (frame.Digest is { } digest)
        {
            writer.WriteInt32(KeyDigest);
            writer.WriteTextString(digest);
        }

        if (frame.Summary is { } summary)
        {
            writer.WriteInt32(KeySummary);
            writer.WriteTextString(summary);
        }

        if (frame.Ttl.HasValue)
        {
            writer.WriteInt32(KeyTtl);
            writer.WriteInt32(frame.Ttl.Value);
        }

        if (frame.Tone.HasValue)
        {
            writer.WriteInt32(KeyTone);
            writer.WriteTextString(SerializeTone(frame.Tone.Value));
        }

        if (frame.Data is { } data)
        {
            writer.WriteInt32(KeyData);
            WriteJsonElement(writer, data);
        }

        if (frame.Profile is { } profile)
        {
            writer.WriteInt32(KeyProfile);
            writer.WriteTextString(profile);
        }

        if (frame.ExpiresAt is { } expiresAt)
        {
            writer.WriteInt32(KeyExpiresAt);
            writer.WriteTextString(expiresAt);
        }

        if (frame.TaskId is { } taskId)
        {
            writer.WriteInt32(KeyTaskId);
            writer.WriteTextString(taskId);
        }

        writer.WriteEndMap();
        return writer.Encode();
    }

    /// <summary>Decode CBOR bytes to a frame.</summary>
    public static QuackFrame Decode(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        var reader = new CborReader(bytes, CborConformanceMode.Lax);
        var frame = new QuackFrame();

        if (reader.ReadStartMap() is not { } mapSize)
            throw new FormatException("Expected CBOR map");

        for (int i = 0; i < mapSize; i++)
        {
            var key = reader.ReadInt32();
            switch (key)
            {
                case KeyVersion: frame = frame with { Version = reader.ReadTextString() }; break;
                case KeyVerb: frame = frame with { Verb = ParseVerb(reader.ReadTextString()) }; break;
                case KeyId: frame = frame with { Id = reader.ReadTextString() }; break;
                case KeyTimestamp: frame = frame with { Timestamp = reader.ReadTextString() }; break;
                case KeySource: frame = frame with { Source = reader.ReadTextString() }; break;
                case KeyDestination: frame = frame with { Destination = reader.ReadTextString() }; break;
                case KeyContext: frame = frame with { Context = reader.ReadTextString() }; break;
                case KeyCorrelation: frame = frame with { Correlation = reader.ReadTextString() }; break;
                case KeyRisk: frame = frame with { Risk = ParseRisk(reader.ReadTextString()) }; break;
                case KeyDigest: frame = frame with { Digest = reader.ReadTextString() }; break;
                case KeySummary: frame = frame with { Summary = reader.ReadTextString() }; break;
                case KeyTtl: frame = frame with { Ttl = reader.ReadInt32() }; break;
                case KeyTone: frame = frame with { Tone = ParseTone(reader.ReadTextString()) }; break;
                case KeyData:
                    var json = ReadJsonElement(reader);
                    frame = frame with { Data = json };
                    break;
                case KeyProfile: frame = frame with { Profile = reader.ReadTextString() }; break;
                case KeyExpiresAt: frame = frame with { ExpiresAt = reader.ReadTextString() }; break;
                case KeyTaskId: frame = frame with { TaskId = reader.ReadTextString() }; break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();
        return frame;
    }

    private static string SerializeRisk(QuackRisk risk) => risk switch
    {
        QuackRisk.None => "none",
        QuackRisk.Low => "low",
        QuackRisk.Medium => "medium",
        QuackRisk.High => "high",
        QuackRisk.Critical => "critical",
        _ => "none",
    };

    private static string SerializeTone(QuackTone tone) => tone switch
    {
        QuackTone.SeriousDuck => "serious-duck",
        QuackTone.PlayfulDuck => "playful-duck",
        QuackTone.AngryGoose => "angry-goose",
        QuackTone.SleepyDuckling => "sleepy-duckling",
        _ => "serious-duck",
    };

    private static QuackVerb ParseVerb(string value) => value.ToLowerInvariant() switch
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
        "dabble" => QuackVerb.Dabble,
        "preen" => QuackVerb.Preen,
        "settle" => QuackVerb.Settle,
        "shun" => QuackVerb.Shun,
        _ => throw new FormatException($"Unknown CBOR verb: {value}"),
    };

    private static QuackRisk ParseRisk(string value) => value.ToLowerInvariant() switch
    {
        "n" or "none" => QuackRisk.None,
        "l" or "low" => QuackRisk.Low,
        "m" or "medium" => QuackRisk.Medium,
        "h" or "high" => QuackRisk.High,
        "c" or "critical" => QuackRisk.Critical,
        _ => QuackRisk.None,
    };

    private static QuackTone ParseTone(string value) => value.ToLowerInvariant() switch
    {
        "serious-duck" => QuackTone.SeriousDuck,
        "playful-duck" => QuackTone.PlayfulDuck,
        "angry-goose" => QuackTone.AngryGoose,
        "sleepy-duckling" => QuackTone.SleepyDuckling,
        _ => QuackTone.SeriousDuck,
    };

    private static void WriteJsonElement(CborWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartMap(null);
                foreach (var prop in element.EnumerateObject())
                {
                    writer.WriteTextString(prop.Name);
                    WriteJsonElement(writer, prop.Value);
                }
                writer.WriteEndMap();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray(null);
                foreach (var item in element.EnumerateArray())
                    WriteJsonElement(writer, item);
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteTextString(element.GetString()!);
                break;
            case JsonValueKind.Number:
                writer.WriteInt64(element.GetInt64());
                break;
            case JsonValueKind.True:
                writer.WriteBoolean(true);
                break;
            case JsonValueKind.False:
                writer.WriteBoolean(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNull();
                break;
        }
    }

    private static JsonElement ReadJsonElement(CborReader reader)
    {
        using var stream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(stream);
        ReadCborToJson(reader, jsonWriter);
        jsonWriter.Flush();
        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream);
        return doc.RootElement.Clone();
    }

    private static void ReadCborToJson(CborReader reader, Utf8JsonWriter writer)
    {
        switch (reader.PeekState())
        {
            case CborReaderState.StartMap:
                {
                    var size = reader.ReadStartMap();
                    writer.WriteStartObject();
                    for (int i = 0; i < size; i++)
                    {
                        writer.WritePropertyName(reader.ReadTextString());
                        ReadCborToJson(reader, writer);
                    }
                    reader.ReadEndMap();
                    writer.WriteEndObject();
                    break;
                }
            case CborReaderState.StartArray:
                {
                    var size = reader.ReadStartArray();
                    writer.WriteStartArray();
                    for (int i = 0; i < size; i++)
                        ReadCborToJson(reader, writer);
                    reader.ReadEndArray();
                    writer.WriteEndArray();
                    break;
                }
            case CborReaderState.TextString:
                writer.WriteStringValue(reader.ReadTextString());
                break;
            case CborReaderState.UnsignedInteger:
            case CborReaderState.NegativeInteger:
                writer.WriteNumberValue(reader.ReadInt64());
                break;
            case CborReaderState.Boolean:
                writer.WriteBooleanValue(reader.ReadBoolean());
                break;
            case CborReaderState.Null:
                reader.ReadNull();
                writer.WriteNullValue();
                break;
            default:
                reader.SkipValue();
                break;
        }
    }
}
