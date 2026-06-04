namespace Quack;

public sealed class HistoryRecordingSink(IQuackSink inner, InMemoryQuackHistory history) : IQuackSink
{
    public async ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct = default)
    {
        var result = await inner.EmitAsync(frame, ct).ConfigureAwait(false);
        if (result.IsSuccess)
            history.Add(result.Frame!);
        return result;
    }
}
