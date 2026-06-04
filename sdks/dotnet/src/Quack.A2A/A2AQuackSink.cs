namespace Quack.A2A;

/// <summary>
/// An <see cref="IQuackSink"/> that delivers frames via an A2A client.
/// This is a placeholder — actual A2A delivery depends on the Microsoft.Agents.AI.A2A SDK.
/// </summary>
public sealed class A2AQuackSink : IQuackSink
{
    /// <inheritdoc />
    public ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct = default) =>
        // Placeholder: actual A2A delivery implementation depends on SDK integration
        new(QuackResult.Success(frame));
}
