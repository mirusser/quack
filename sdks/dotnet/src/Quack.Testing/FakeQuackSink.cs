using System.Text.Json;

namespace Quack.Testing;

/// <summary>
/// Records all emitted frames for test verification.
/// </summary>
public sealed class FakeQuackSink : IQuackSink
{
    private readonly List<QuackFrame> _emitted = new();

    /// <summary>Frames emitted through this sink.</summary>
    public IReadOnlyList<QuackFrame> EmittedFrames => _emitted;

    /// <inheritdoc />
    public ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct = default)
    {
        _emitted.Add(frame);
        return new(QuackResult.Success(frame));
    }
}
