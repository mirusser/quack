namespace Quack;

/// <summary>
/// Protocol rules enforced by Quack-core.
/// Each enum value maps to a § rule in the protocol spec.
/// </summary>
public enum QuackRule
{
    /// <summary>§3.1.1 — flap requires prior hatch + bob with same correlation.</summary>
    FlapWithoutHatch,
    /// <summary>§3.1.2 — hatch must reference a prior egg via correlation.</summary>
    HatchWithoutEgg,
    /// <summary>§3.1.3 — egg requires a prior splash in the same context.</summary>
    EggWithoutSplash,
    /// <summary>§3.1.4 — egg must carry a digest (proof of artifact).</summary>
    EggWithoutProof,
    /// <summary>§3.2.5 — honk must include data.reason.</summary>
    HonkWithoutReason,
    /// <summary>§3.2.6 — molt must carry a non-null correlation.</summary>
    MoltWithoutCorrelation,
    /// <summary>§3.2.7 — splash must include data.evidence (array, ≥1 entry).</summary>
    SplashWithoutEvidence,
    /// <summary>§3.2.8 — non-broadcast verb must carry destination.</summary>
    NonBroadcastWithoutDestination,
    /// <summary>Risk > MaxRisk (policy gate, not protocol rule).</summary>
    RiskExceedsMax,
    /// <summary>TTL > MaxTtl (policy gate, not protocol rule).</summary>
    TtlExceedsMax,
    /// <summary>Catch-all for malformed frames.</summary>
    InvalidFrame,
}
