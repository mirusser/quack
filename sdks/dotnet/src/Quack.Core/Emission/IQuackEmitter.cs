namespace Quack;

/// <summary>
/// Application-facing facade for emitting Quack frames.
/// Wraps a sink pipeline and auto-sets source/timestamp/id.
/// </summary>
public interface IQuackEmitter
{
    /// <summary>The name of this agent.</summary>
    string AgentName { get; }

    /// <summary>Validate and emit a frame through the sink pipeline.</summary>
    ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct = default);
}
