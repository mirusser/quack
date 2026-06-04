namespace Quack;

/// <summary>
/// Risk level for gating delivery at the protocol level.
/// Ordered: None &lt; Low &lt; Medium &lt; High &lt; Critical.
/// Wire values are the lowercase names: "none", "low", "medium", "high", "critical".
/// </summary>
public enum QuackRisk
{
    /// <summary>none — No risk. Informational.</summary>
    None = 0,
    /// <summary>low — Minimal impact. Routine.</summary>
    Low = 1,
    /// <summary>medium — Moderate impact. Requires attention.</summary>
    Medium = 2,
    /// <summary>high — Significant impact. Requires approval.</summary>
    High = 3,
    /// <summary>critical — Severe impact. Maximum scrutiny.</summary>
    Critical = 4,
}
