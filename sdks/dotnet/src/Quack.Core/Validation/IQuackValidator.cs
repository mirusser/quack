namespace Quack;

/// <summary>
/// Validates Quack frames against protocol rules (§3 of the spec).
/// Supports both stateless structural checks and stateful sequencing checks.
/// </summary>
public interface IQuackValidator
{
    /// <summary>Stateless validation — structural rules only (§3.2.5–§3.2.8).</summary>
    ValidationResult Validate(QuackFrame frame);

    /// <summary>Stateful validation — includes sequencing rules (§3.1.1–§3.1.4).</summary>
    ValidationResult Validate(QuackFrame frame, IQuackHistory history);
}
