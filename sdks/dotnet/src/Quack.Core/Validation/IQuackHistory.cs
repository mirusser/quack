namespace Quack;

/// <summary>
/// Read-only history of emitted frames. Implementations can be in-memory
/// (for tests) or persisted (for production).
/// </summary>
public interface IQuackHistory
{
    /// <summary>Get all frames sharing the same correlation ID.</summary>
    IReadOnlyList<QuackFrame> GetByCorrelation(string correlation);

    /// <summary>Get all frames sharing the same context.</summary>
    IReadOnlyList<QuackFrame> GetByContext(string context);
}
