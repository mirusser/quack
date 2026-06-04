namespace Quack;

/// <summary>
/// The fundamental delivery contract. Every delivery target implements this.
/// Must validate before delivery and return QuackResult, never throw for protocol rejections.
/// </summary>
public interface IQuackSink
{
    /// <summary>Validate and deliver a frame. Returns success or structured rejection.</summary>
    ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct = default);
}
