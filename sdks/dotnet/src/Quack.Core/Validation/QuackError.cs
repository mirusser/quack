namespace Quack;

/// <summary>
/// A structured protocol error. Carries a machine-readable code,
/// human-readable message, and the rule that was violated.
/// </summary>
public readonly struct QuackError(string code, string message, QuackRule rule)
{
    /// <summary>Machine-readable error code (e.g. "FLAP_WITHOUT_HATCH").</summary>
    public string Code { get; } = code;

    /// <summary>Human-readable description.</summary>
    public string Message { get; } = message;

    /// <summary>The protocol rule that was violated.</summary>
    public QuackRule Rule { get; } = rule;
}
