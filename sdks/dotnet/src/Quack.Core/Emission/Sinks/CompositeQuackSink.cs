namespace Quack;

/// <summary>
/// Controls how <see cref="CompositeQuackSink"/> handles inner sink failures.
/// </summary>
public enum CompositeFailureMode
{
    /// <summary>Try all sinks, return the first error.</summary>
    Continue,
    /// <summary>Abort on the first error.</summary>
    Stop,
}

/// <summary>
/// Fans out delivery to an ordered list of inner sinks.
/// Each inner sink receives the same validated frame.
/// </summary>
public sealed class CompositeQuackSink(
    IEnumerable<IQuackSink> sinks,
    CompositeFailureMode failureMode = CompositeFailureMode.Continue) : IQuackSink
{
    private readonly IReadOnlyList<IQuackSink> _sinks = sinks.ToList();
    private readonly CompositeFailureMode _failureMode = failureMode;

    /// <summary>
    /// Convenience constructor for a fixed set of sinks.
    /// </summary>
    public CompositeQuackSink(params IQuackSink[] sinks)
        : this(sinks.AsEnumerable(), CompositeFailureMode.Continue) { }

    /// <inheritdoc />
    public async ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct = default)
    {
        QuackResult? firstError = null;

        foreach (var sink in _sinks)
        {
            var result = await sink.EmitAsync(frame, ct).ConfigureAwait(false);
            if (result.IsRejected)
            {
                firstError ??= result;
                if (_failureMode == CompositeFailureMode.Stop)
                    return result;
            }
        }

        return firstError ?? QuackResult.Success(frame);
    }
}
