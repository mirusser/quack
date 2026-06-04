using Microsoft.AspNetCore.Http;

namespace Quack.AspNetCore;

/// <summary>
/// Extension methods for reading and writing Quack frames on <see cref="HttpContext"/>.
/// </summary>
public static class QuackHttpExtensions
{
    /// <summary>Get the inbound Quack frame from the current request.</summary>
    public static QuackFrame? GetQuackFrame(this HttpContext context)
    {
        return context.Features.Get<IQuackFeature>()?.RequestFrame;
    }

    /// <summary>Attach a Quack frame to the current response.</summary>
    public static void AttachQuackFrame(this HttpContext context, QuackFrame frame)
    {
        context.Features.Get<IQuackFeature>()?.Attach(frame);
    }

    /// <summary>Try to read a Quack frame from the request header.</summary>
    public static bool TryGetQuackFrame(this HttpRequest request, out QuackFrame frame)
    {
        if (request.HttpContext.Features.Get<IQuackFeature>()?.RequestFrame is { } f)
        {
            frame = f;
            return true;
        }

        frame = default!;
        return false;
    }
}
