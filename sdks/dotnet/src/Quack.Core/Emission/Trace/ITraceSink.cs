namespace Quack;

/// <summary>
/// Trace sink for observability. Separate from delivery — trace failures
/// must never affect EmitAsync callers.
/// </summary>
public interface ITraceSink
{
    /// <summary>Write a delivered frame to the trace stream. Fire-and-forget.</summary>
    void Write(QuackFrame frame);

    /// <summary>Write a rejection to the trace stream. Fire-and-forget.</summary>
    void WriteRejection(QuackFrame frame, QuackResult result);

    /// <summary>
    /// Write a machine-readable structured trace line (no emoji, fixed-width columns).
    /// Per spec adapters §6. Fire-and-forget.
    /// </summary>
    void WriteStructured(QuackFrame frame);
}
