using System.Text.Json;

namespace Quack.Mutation;

public abstract record MutationFrameBase
{
    public string? Id { get; init; }
    public string? Timestamp { get; init; }
    public string? Source { get; init; }
    public string? Destination { get; init; }
    public string? Context { get; init; }
    public string? TaskId { get; init; }
    public string? Correlation { get; init; }
    public QuackRisk Risk { get; init; } = QuackRisk.None;
    public string? ExpiresAt { get; init; }
    public string? Summary { get; init; }
    public string? Ttl { get; init; }

    public abstract QuackFrame ToQuackFrame();

    public const string MutationProfile = "quack-mutation-v0";

    protected QuackFrame BuildBase(QuackVerb verb)
    {
        return new QuackFrame
        {
            Version = "0.1",
            Profile = MutationProfile,
            Verb = verb,
            Id = Id ?? QuackId.NewId(),
            Timestamp = Timestamp ?? DateTimeOffset.UtcNow.ToString("o"),
            Source = Source ?? string.Empty,
            Destination = Destination,
            Context = Context,
            TaskId = TaskId,
            Correlation = Correlation,
            Risk = Risk,
            ExpiresAt = ExpiresAt,
            Summary = Summary,
            Ttl = int.TryParse(Ttl, out var t) ? t : null,
        };
    }

    protected T ApplyBase<T>(QuackFrame frame) where T : MutationFrameBase
    {
        var clone = this with
        {
            Id = frame.Id,
            Timestamp = frame.Timestamp,
            Source = frame.Source,
            Destination = frame.Destination,
            Context = frame.Context,
            TaskId = frame.TaskId,
            Correlation = frame.Correlation,
            Risk = frame.Risk,
            ExpiresAt = frame.ExpiresAt,
            Summary = frame.Summary,
            Ttl = frame.Ttl?.ToString(),
        };
        return (T)clone;
    }

    protected static JsonElement? WriteData(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        write(writer);
        writer.WriteEndObject();
        writer.Flush();
        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream);
        return doc.RootElement.Clone();
    }

    protected static void WriteDictionary(Utf8JsonWriter writer, string name, Dictionary<string, object> dict)
    {
        writer.WriteStartObject(name);
        foreach (var (key, value) in dict)
        {
            if (value is string s) writer.WriteString(key, s);
            else if (value is int i) writer.WriteNumber(key, i);
            else if (value is bool b) writer.WriteBoolean(key, b);
            else if (value is null) writer.WriteNull(key);
            else writer.WriteString(key, value.ToString()!);
        }
        writer.WriteEndObject();
    }

    protected static void WriteEvidenceArtifacts(Utf8JsonWriter writer, EvidenceArtifact[] artifacts)
    {
        writer.WriteStartArray("evidenceArtifacts");
        foreach (var a in artifacts)
        {
            writer.WriteStartObject();
            writer.WriteString("kind", a.Kind);
            writer.WriteString("digest", a.Digest);
            if (a.Uri is not null) writer.WriteString("uri", a.Uri);
            if (a.MediaType is not null) writer.WriteString("mediaType", a.MediaType);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    protected static EvidenceArtifact[] ReadEvidenceArtifacts(JsonElement element)
    {
        return element.EnumerateArray().Select(a => new EvidenceArtifact
        {
            Kind = a.GetProperty("kind").GetString()!,
            Digest = a.GetProperty("digest").GetString()!,
            Uri = ReadOptionalStringProp(a, "uri"),
            MediaType = ReadOptionalStringProp(a, "mediaType"),
        }).ToArray();
    }

    protected static Dictionary<string, object> ReadDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object>();
        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString()!,
                JsonValueKind.Number => (object)prop.Value.GetInt64(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null!,
                _ => prop.Value.GetRawText(),
            };
        }
        return dict;
    }

    protected static string ReadStringProp(JsonElement element, string name) =>
        element.GetProperty(name).GetString()!;

    protected static string? ReadOptionalStringProp(JsonElement element, string name) =>
        element.TryGetProperty(name, out var val) && val.ValueKind != JsonValueKind.Null
            ? val.GetString() : null;
}
