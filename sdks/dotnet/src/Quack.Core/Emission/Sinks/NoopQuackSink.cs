namespace Quack;

/// <summary>
/// A sink that discards all frames. Always returns Success.
/// Useful as a default or test placeholder.
/// </summary>
public sealed class NoopQuackSink : IQuackSink
{
    /// <inheritdoc />
    public ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct = default) =>
        new(QuackResult.Success(frame));
}
