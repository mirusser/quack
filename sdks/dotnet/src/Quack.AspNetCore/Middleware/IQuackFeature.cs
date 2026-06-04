namespace Quack.AspNetCore;

/// <summary>
/// Carries Quack frames through the HTTP request/response pipeline.
/// </summary>
public interface IQuackFeature
{
    /// <summary>The inbound Quack frame parsed from the request header, if any.</summary>
    QuackFrame? RequestFrame { get; }

    /// <summary>Frames attached during request processing for the response header.</summary>
    IReadOnlyList<QuackFrame> ResponseFrames { get; }

    /// <summary>Attach a frame to the response.</summary>
    void Attach(QuackFrame frame);
}
