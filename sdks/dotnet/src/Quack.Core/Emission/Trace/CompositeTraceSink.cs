namespace Quack;

/// <summary>
/// Fans out trace writes to an ordered list of inner trace sinks.
/// </summary>
public sealed class CompositeTraceSink : ITraceSink
{
    private readonly IReadOnlyList<ITraceSink> _sinks;

    /// <param name="sinks">Inner trace sinks.</param>
    public CompositeTraceSink(IEnumerable<ITraceSink> sinks) => _sinks = sinks.ToList();

    /// <inheritdoc />
    public void Write(QuackFrame frame)
    {
        foreach (var sink in _sinks)
        {
            try { sink.Write(frame); } catch { /* fire-and-forget */ }
        }
    }

    /// <inheritdoc />
    public void WriteRejection(QuackFrame frame, QuackResult result)
    {
        foreach (var sink in _sinks)
        {
            try { sink.WriteRejection(frame, result); } catch { /* fire-and-forget */ }
        }
    }
}
