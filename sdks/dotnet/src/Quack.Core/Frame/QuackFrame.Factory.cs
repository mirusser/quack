using System.Text.Json;

namespace Quack;

public sealed partial record QuackFrame
{
    /// <summary>
    /// A digest-bound evidence reference used in splash frames.
    /// </summary>
    public sealed record EvidenceRef
    {
        /// <summary>Evidence kind (e.g. "k8s.events").</summary>
        public string Kind { get; init; } = string.Empty;

        /// <summary>Content digest (e.g. "sha256:abc...").</summary>
        public string Digest { get; init; } = string.Empty;

        /// <summary>Location URI (e.g. "artifact://events/default/web").</summary>
        public string? Uri { get; init; }

        /// <summary>Optional media type.</summary>
        public string? MediaType { get; init; }
    }

    private static QuackFrame Create(
        QuackVerb verb,
        string source,
        string? destination,
        string? context,
        string? correlation,
        QuackRisk risk,
        string? summary,
        QuackTone? tone,
        JsonElement? data = null,
        string? digest = null,
        int? ttl = null)
    {
        return new QuackFrame
        {
            Version = 1,
            Verb = verb,
            Id = QuackId.NewId(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Source = source,
            Destination = destination,
            Context = context,
            Correlation = correlation,
            Risk = risk,
            Summary = summary,
            Tone = tone,
            Data = data,
            Digest = digest,
            Ttl = ttl,
        };
    }

    private static JsonElement JsonObject(params (string Key, JsonElement Value)[] properties)
    {
        using var doc = JsonDocument.Parse("{}");
        // We need to build a JsonElement from scratch — use Utf8JsonWriter
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        foreach (var (key, value) in properties)
        {
            writer.WritePropertyName(key);
            value.WriteTo(writer);
        }
        writer.WriteEndObject();
        writer.Flush();
        stream.Position = 0;
        using var resultDoc = JsonDocument.Parse(stream);
        return resultDoc.RootElement.Clone();
    }

    private static JsonElement JsonString(string value)
    {
        using var doc = JsonDocument.Parse($"\"{value}\"");
        return doc.RootElement.Clone();
    }

    private static JsonElement JsonArray(params JsonElement[] elements)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartArray();
        foreach (var element in elements)
            element.WriteTo(writer);
        writer.WriteEndArray();
        writer.Flush();
        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream);
        return doc.RootElement.Clone();
    }

    private static JsonElement EvidenceToJson(QuackFrame.EvidenceRef evidence)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        writer.WriteString("kind", evidence.Kind);
        writer.WriteString("digest", evidence.Digest);
        if (evidence.Uri is not null)
            writer.WriteString("uri", evidence.Uri);
        if (evidence.MediaType is not null)
            writer.WriteString("mediaType", evidence.MediaType);
        writer.WriteEndObject();
        writer.Flush();
        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream);
        return doc.RootElement.Clone();
    }

    // ── Announce-type (may omit destination per §1.4) ──

    /// <summary>🦆 Announce — minimum: source.</summary>
    public static QuackFrame Quack(
        string source,
        string? destination = null,
        string? context = null,
        string? correlation = null,
        QuackRisk risk = QuackRisk.None,
        string? summary = null,
        QuackTone? tone = null) =>
        Create(QuackVerb.Quack, source, destination, context, correlation, risk, summary, tone);

    /// <summary>🪿 Warning — minimum: source, reason.</summary>
    public static QuackFrame Honk(
        string source,
        string reason,
        string? destination = null,
        string? context = null,
        string? correlation = null,
        QuackRisk risk = QuackRisk.None,
        string? summary = null,
        QuackTone? tone = null) =>
        Create(QuackVerb.Honk, source, destination, context, correlation, risk, summary, tone,
            data: JsonObject(("reason", JsonString(reason))));

    /// <summary>💦 Attach evidence — minimum: source, evidence.</summary>
    public static QuackFrame Splash(
        string source,
        QuackFrame.EvidenceRef[] evidence,
        string? destination = null,
        string? context = null,
        string? correlation = null,
        string? summary = null,
        QuackTone? tone = null) =>
        Create(QuackVerb.Splash, source, destination, context, correlation, QuackRisk.None, summary, tone,
            data: JsonObject(("evidence", JsonArray(evidence.Select(EvidenceToJson).ToArray()))));

    /// <summary>🪹 Canceled — minimum: source, correlation.</summary>
    public static QuackFrame Molt(
        string source,
        string correlation,
        string? destination = null,
        string? context = null,
        string? reason = null,
        string? summary = null,
        QuackTone? tone = null)
    {
        JsonElement? data = reason is not null
            ? JsonObject(("reason", JsonString(reason)))
            : null;
        return Create(QuackVerb.Molt, source, destination, context, correlation, QuackRisk.None, summary, tone, data: data);
    }

    // ── Request-type (require destination per §1.4) ──

    /// <summary>🐤 Request — minimum: source, destination.</summary>
    public static QuackFrame Peck(
        string source,
        string destination,
        string? context = null,
        string? correlation = null,
        QuackRisk risk = QuackRisk.None,
        string? summary = null,
        QuackTone? tone = null) =>
        Create(QuackVerb.Peck, source, destination, context, correlation, risk, summary, tone);

    /// <summary>🥚 Produced plan — minimum: source, destination, eggId, digest.</summary>
    public static QuackFrame Egg(
        string source,
        string destination,
        string eggId,
        string digest,
        string? context = null,
        string? correlation = null,
        QuackRisk risk = QuackRisk.None,
        string? summary = null,
        int? ttl = null,
        QuackTone? tone = null) =>
        Create(QuackVerb.Egg, source, destination, context, correlation, risk, summary, tone,
            data: JsonObject(("eggId", JsonString(eggId))),
            digest: digest,
            ttl: ttl);

    /// <summary>🐣 Approval requested — minimum: source, destination, eggId, correlation.</summary>
    public static QuackFrame Hatch(
        string source,
        string destination,
        string eggId,
        string correlation,
        string? context = null,
        QuackRisk risk = QuackRisk.None,
        string? summary = null,
        int? ttl = null,
        QuackTone? tone = null) =>
        Create(QuackVerb.Hatch, source, destination, context, correlation, risk, summary, tone,
            data: JsonObject(("eggId", JsonString(eggId))),
            ttl: ttl);

    /// <summary>🪽 Execute — minimum: source, destination, eggId, digest, correlation.</summary>
    public static QuackFrame Flap(
        string source,
        string destination,
        string eggId,
        string digest,
        string correlation,
        string? context = null,
        QuackRisk risk = QuackRisk.None,
        string? summary = null,
        QuackTone? tone = null) =>
        Create(QuackVerb.Flap, source, destination, context, correlation, risk, summary, tone,
            data: JsonObject(("eggId", JsonString(eggId))),
            digest: digest);

    // ── Response-type (require destination per §1.4) ──

    /// <summary>🦢 Acknowledge / Approved — minimum: source, destination, correlation.</summary>
    public static QuackFrame Bob(
        string source,
        string destination,
        string correlation,
        string? context = null,
        string? summary = null,
        QuackTone? tone = null) =>
        Create(QuackVerb.Bob, source, destination, context, correlation, QuackRisk.None, summary, tone);

    /// <summary>🐦‍⬛ Reject — minimum: source, destination, correlation.</summary>
    public static QuackFrame Nack(
        string source,
        string destination,
        string correlation,
        string? context = null,
        string? summary = null,
        QuackTone? tone = null) =>
        Create(QuackVerb.Nack, source, destination, context, correlation, QuackRisk.None, summary, tone);

    /// <summary>🕊️ Completed — minimum: source, destination, correlation.</summary>
    public static QuackFrame Perch(
        string source,
        string destination,
        string correlation,
        string? context = null,
        string? summary = null,
        QuackTone? tone = null) =>
        Create(QuackVerb.Perch, source, destination, context, correlation, QuackRisk.None, summary, tone);
}
