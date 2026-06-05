namespace Quack.A2A;

/// <summary>
/// An <see cref="IQuackSink"/> that delivers frames via a caller-supplied A2A dispatch delegate.
/// Wire this to your A2A client once the Microsoft.Agents.AI.A2A SDK is available,
/// or to any async send function that returns a delivery result.
/// </summary>
public sealed class A2AQuackSink(Func<QuackFrame, CancellationToken, ValueTask<QuackResult>> dispatch) : IQuackSink
{
    private readonly Func<QuackFrame, CancellationToken, ValueTask<QuackResult>> _dispatch = dispatch;

    /// <inheritdoc />
    public ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct = default) =>
        _dispatch(frame, ct);
}
