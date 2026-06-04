namespace Quack;

public sealed class InMemoryQuackHistory : IQuackHistory
{
    private readonly List<QuackFrame> _frames = [];

    public void Add(QuackFrame frame) => _frames.Add(frame);

    public IReadOnlyList<QuackFrame> GetByCorrelation(string correlation) =>
        _frames.Where(f => f.Correlation == correlation).ToList();

    public IReadOnlyList<QuackFrame> GetByContext(string context) =>
        _frames.Where(f => f.Context == context).ToList();
}
