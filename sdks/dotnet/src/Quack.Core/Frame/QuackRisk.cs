namespace Quack;

/// <summary>
/// Risk level for gating delivery at the protocol level.
/// Ordered: None &lt; Low &lt; Medium &lt; High &lt; Critical.
/// </summary>
public enum QuackRisk
{
    /// <summary>n — No risk. Informational.</summary>
    None = 0,
    /// <summary>l — Minimal impact. Routine.</summary>
    Low = 1,
    /// <summary>m — Moderate impact. Requires attention.</summary>
    Medium = 2,
    /// <summary>h — Significant impact. Requires approval.</summary>
    High = 3,
    /// <summary>c — Severe impact. Maximum scrutiny.</summary>
    Critical = 4,
}
