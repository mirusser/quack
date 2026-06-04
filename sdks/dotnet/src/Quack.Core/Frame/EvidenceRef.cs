namespace Quack;

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
