using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Quack.AspNetCore;

/// <summary>
/// Extension methods for registering Quack middleware in the ASP.NET Core pipeline.
/// </summary>
public static class QuackMiddlewareExtensions
{
    /// <summary>Adds Quack header injection middleware to the pipeline.</summary>
    public static IApplicationBuilder UseQuack(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<QuackMiddleware>();
    }
}

internal sealed class QuackMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var feature = new QuackFeature();

        // Read inbound Quack header
        if (context.Request.Headers.TryGetValue("Quack", out var quackHeader))
        {
            try
            {
                feature.RequestFrame = QuackHttp.DecodeHeader(quackHeader.ToString());
            }
            catch
            {
                // Malformed header — don't block the request
            }
        }

        context.Features.Set<IQuackFeature>(feature);

        await _next(context).ConfigureAwait(false);

        // Write outbound Quack headers
        if (feature.ResponseFrames.Count > 0)
        {
            var frame = feature.ResponseFrames[^1]; // last frame
            context.Response.Headers["Quack"] = QuackHttp.EncodeHeader(frame);

            if (frame.Context is not null || frame.Correlation is not null)
            {
                context.Response.Headers["Quack-Trace"] = QuackHttp.EncodeTraceHeader(frame);
            }
        }
    }
}
