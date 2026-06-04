namespace Quack;

/// <summary>
/// Configuration options for the Quack emitter.
/// </summary>
public sealed class QuackOptions
{
    /// <summary>The name of this agent. Used as default source.</summary>
    public string AgentName { get; set; } = "quack-agent";

    /// <summary>Maximum accepted risk. Frames exceeding this are rejected.</summary>
    public QuackRisk MaxRisk { get; set; } = QuackRisk.High;

    /// <summary>Maximum accepted TTL in ms. Frames exceeding this are rejected.</summary>
    public int? MaxTtl { get; set; }

    /// <summary>Maximum accepted Quack protocol version as a semantic version string (e.g. "0.1").</summary>
    public string MaxQuackVersion { get; set; } = "0.1";
}
