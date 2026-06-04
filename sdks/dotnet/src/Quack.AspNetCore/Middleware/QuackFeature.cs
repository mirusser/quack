namespace Quack.AspNetCore;

internal sealed class QuackFeature : IQuackFeature
{
    private readonly List<QuackFrame> _responseFrames = new();

    public QuackFrame? RequestFrame { get; set; }
    public IReadOnlyList<QuackFrame> ResponseFrames => _responseFrames;

    public void Attach(QuackFrame frame) => _responseFrames.Add(frame);
}
