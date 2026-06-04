namespace Quack;

/// <summary>
/// The result of frame validation. Carries either success or a list of errors.
/// </summary>
public readonly struct ValidationResult
{
    /// <summary>True if the frame passed all applicable rules.</summary>
    public bool IsValid { get; }

    /// <summary>Structured errors for each violated rule.</summary>
    public IReadOnlyList<QuackError> Errors { get; }

    private ValidationResult(bool isValid, IReadOnlyList<QuackError> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    /// <summary>Create a successful validation result.</summary>
    public static ValidationResult Valid() => new(true, Array.Empty<QuackError>());

    /// <summary>Create a failed validation result with errors.</summary>
    public static ValidationResult Invalid(params QuackError[] errors) =>
        new(false, errors);
}
