using System.Text.Json;

namespace Quack.Negotiate;

public abstract record NegotiateFrameBase
{
    public const string NegotiateProfile = "quack-negotiate-v0";

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

    public abstract QuackFrame ToQuackFrame();

    protected QuackFrame BuildBase(QuackVerb verb)
    {
        return new QuackFrame
        {
            Version = "0.1",
            Profile = NegotiateProfile,
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
        };
    }

    protected T ApplyBase<T>(QuackFrame frame) where T : NegotiateFrameBase
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

    protected static void WriteDict(Utf8JsonWriter w, string name, Dictionary<string, object> dict)
    {
        w.WriteStartObject(name);
        foreach (var (k, v) in dict)
        {
            if (v is string s) w.WriteString(k, s);
            else if (v is int i) w.WriteNumber(k, i);
            else if (v is bool b) w.WriteBoolean(k, b);
            else w.WriteString(k, v.ToString()!);
        }
        w.WriteEndObject();
    }

    protected static void WriteStringArray(Utf8JsonWriter w, string name, string[] arr)
    {
        w.WriteStartArray(name);
        foreach (var s in arr) w.WriteStringValue(s);
        w.WriteEndArray();
    }

    protected static Dictionary<string, object> ReadDict(JsonElement el)
    {
        var d = new Dictionary<string, object>();
        foreach (var p in el.EnumerateObject())
            d[p.Name] = p.Value.ValueKind switch
            {
                JsonValueKind.String => p.Value.GetString()!,
                JsonValueKind.Number => p.Value.GetInt64(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => p.Value.GetRawText(),
            };
        return d;
    }

    protected static string[] ReadStringArray(JsonElement el) =>
        el.EnumerateArray().Select(e => e.GetString()!).ToArray();

    protected static string? ReadOptStr(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null ? v.GetString() : null;
}
