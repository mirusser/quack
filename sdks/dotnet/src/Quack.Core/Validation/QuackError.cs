namespace Quack;

/// <summary>
/// A structured protocol error. Carries a machine-readable code,
/// human-readable message, and the rule that was violated.
/// </summary>
public readonly struct QuackError
{
    /// <summary>Machine-readable error code (e.g. "FLAP_WITHOUT_HATCH").</summary>
    public string Code { get; }

    /// <summary>Human-readable description.</summary>
    public string Message { get; }

    /// <summary>The protocol rule that was violated.</summary>
    public QuackRule Rule { get; }

    public QuackError(string code, string message, QuackRule rule)
    {
        Code = code;
        Message = message;
        Rule = rule;
    }
}
