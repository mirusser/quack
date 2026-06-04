namespace Quack;

/// <summary>
/// The result of an EmitAsync call. Either success with the emitted frame,
/// or rejection with structured errors. Never throws for protocol rejections.
/// </summary>
public readonly struct QuackResult
{
    /// <summary>True if the frame was accepted and delivered.</summary>
    public bool IsSuccess { get; }

    /// <summary>True if the frame was rejected by validation or policy.</summary>
    public bool IsRejected => !IsSuccess;

    /// <summary>The emitted frame, if successful.</summary>
    public QuackFrame? Frame { get; }

    /// <summary>Structured errors explaining the rejection.</summary>
    public IReadOnlyList<QuackError> Errors { get; }

    private QuackResult(bool isSuccess, QuackFrame? frame, IReadOnlyList<QuackError> errors)
    {
        IsSuccess = isSuccess;
        Frame = frame;
        Errors = errors;
    }

    /// <summary>Create a success result carrying the emitted frame.</summary>
    public static QuackResult Success(QuackFrame frame) =>
        new(true, frame, Array.Empty<QuackError>());

    /// <summary>Create a rejection result with structured errors.</summary>
    public static QuackResult Rejected(params QuackError[] errors) =>
        new(false, null, errors);
}
