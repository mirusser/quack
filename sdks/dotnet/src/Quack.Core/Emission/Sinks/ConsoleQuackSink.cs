namespace Quack;

/// <summary>
/// Writes frames to the console as Quack-Text lines.
/// Low/Medium risk → stdout; High/Critical → stderr.
/// </summary>
public sealed class ConsoleQuackSink : IQuackSink
{
    /// <inheritdoc />
    public ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct = default)
    {
        var text = QuackText.Encode(frame);
        if (frame.Risk >= QuackRisk.High)
            Console.Error.WriteLine(text);
        else
            Console.WriteLine(text);
        return new(QuackResult.Success(frame));
    }
}
