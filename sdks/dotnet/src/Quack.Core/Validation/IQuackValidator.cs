namespace Quack;

/// <summary>
/// Validates Quack frames against protocol rules (§3 of the spec).
/// The one-argument overload is structural-only; the history-aware overload is full protocol validation.
/// </summary>
public interface IQuackValidator
{
    /// <summary>
    /// Validates only the frame's standalone structure, including required fields,
    /// verb-specific payload shape, and destination requirements (§3.2.5–§3.2.8).
    /// This overload does not evaluate protocol sequencing rules that require prior frames.
    /// </summary>
    ValidationResult Validate(QuackFrame frame);

    /// <summary>
    /// Validates the full protocol state for a frame by first applying structural checks,
    /// then evaluating sequencing rules against <paramref name="history" />
    /// such as splash-before-egg, hatch-after-egg, and flap-after-hatch-and-bob (§3.1.1–§3.1.4).
    /// </summary>
    ValidationResult Validate(QuackFrame frame, IQuackHistory history);
}
