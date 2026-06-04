namespace Quack;

/// <summary>
/// An immutable Quack frame — the canonical abstract model.
/// All encodings round-trip through this type.
/// </summary>
public sealed partial record QuackFrame
{
    /// <summary>Protocol version as a semantic version string ("0.1" for v0.1).</summary>
    public string Version { get; init; } = "0.1";

    /// <summary>The semantic verb for this frame.</summary>
    public QuackVerb Verb { get; init; }

    /// <summary>ULID frame identifier — unique per frame.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>ISO 8601 UTC timestamp when the frame was created.</summary>
    public string? Timestamp { get; init; }

    /// <summary>Emitting agent or component.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>Target agent. Omitted = pond-wide broadcast.</summary>
    public string? Destination { get; init; }

    /// <summary>Logical grouping scope (e.g. "k8s/default/web").</summary>
    public string? Context { get; init; }

    /// <summary>Links frames into a sequence.</summary>
    public string? Correlation { get; init; }

    /// <summary>Risk level for delivery gating.</summary>
    public QuackRisk Risk { get; init; }

    /// <summary>Human-readable one-liner summary.</summary>
    public string? Summary { get; init; }

    /// <summary>Digest binding (e.g. "sha256:abc...").</summary>
    public string? Digest { get; init; }

    /// <summary>Relative freshness window in milliseconds from Timestamp.</summary>
    public int? Ttl { get; init; }

    /// <summary>Decorative tone. No semantic effect.</summary>
    public QuackTone? Tone { get; init; }

    /// <summary>
    /// Profile identifier for profile extensions (e.g. "quack-mutation-v0", "quack-negotiate-v0").
    /// Omitted for core Quack frames.
    /// </summary>
    public string? Profile { get; init; }

    /// <summary>
    /// ISO 8601 UTC freshness bound for this frame. Profile-specific alternative to Ttl.
    /// A receiver treats an expiresAt in the past as an elapsed TTL — staleness, not rejection.
    /// </summary>
    public string? ExpiresAt { get; init; }

    /// <summary>A2A task scope. Set when the frame belongs to a specific A2A task.</summary>
    public string? TaskId { get; init; }

    /// <summary>Verb-structured payload as a JSON element map.</summary>
    public JsonElement? Data { get; init; }
}
