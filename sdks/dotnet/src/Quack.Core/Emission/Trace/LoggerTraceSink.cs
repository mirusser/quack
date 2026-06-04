using Microsoft.Extensions.Logging;

namespace Quack;

/// <summary>
/// Writes trace frames to an <see cref="ILogger"/> as emoji-prefixed Quack-Text lines.
/// </summary>
public sealed class LoggerTraceSink(ILogger logger) : ITraceSink
{
    private readonly ILogger _logger = logger;

    /// <inheritdoc />
    public void Write(QuackFrame frame)
    {
        var emoji = EmojiFor(frame.Verb);
        _logger.LogInformation("{Emoji} {QuackFrame}", emoji, QuackText.Encode(frame));
    }

    /// <inheritdoc />
    public void WriteRejection(QuackFrame frame, QuackResult result)
    {
        Write(frame);
        if (result.Errors.Count > 0)
        {
            var firstError = result.Errors[0];
            var nack = QuackFrame.Nack(
                source: "quack-core",
                destination: frame.Source,
                correlation: frame.Correlation ?? frame.Id,
                summary: $"{firstError.Code}: {firstError.Message}");
            Write(nack);
        }
    }

    /// <inheritdoc />
    public void WriteStructured(QuackFrame frame) =>
        _logger.LogInformation("{StructuredTrace}", QuackTraceFormatter.FormatStructured(frame));

    internal static string EmojiFor(QuackVerb verb) => verb switch
    {
        QuackVerb.Quack => "🦆",
        QuackVerb.Peck => "🐤",
        QuackVerb.Bob => "🦢",
        QuackVerb.Nack => "🐦‍⬛",
        QuackVerb.Egg => "🥚",
        QuackVerb.Hatch => "🐣",
        QuackVerb.Flap => "🪽",
        QuackVerb.Perch => "🕊️",
        QuackVerb.Honk => "🪿",
        QuackVerb.Molt => "🪹",
        QuackVerb.Splash => "💦",
        QuackVerb.Dabble => "🔍",
        QuackVerb.Preen => "🪶",
        QuackVerb.Settle => "✅",
        QuackVerb.Shun => "🚫",
        _ => "❓",
    };
}

/// <summary>
/// Typed variant of <see cref="LoggerTraceSink"/> using <see cref="ILogger{T}"/>.
/// </summary>
public sealed class LoggerTraceSink<T>(ILogger<T> logger) : ITraceSink
{
    private readonly ILogger<T> _logger = logger;

    /// <inheritdoc />
    public void Write(QuackFrame frame)
    {
        var emoji = LoggerTraceSink.EmojiFor(frame.Verb);
        _logger.LogInformation("{Emoji} {QuackFrame}", emoji, QuackText.Encode(frame));
    }

    /// <inheritdoc />
    public void WriteRejection(QuackFrame frame, QuackResult result)
    {
        Write(frame);
        if (result.Errors.Count > 0)
        {
            var firstError = result.Errors[0];
            var nack = QuackFrame.Nack(
                source: "quack-core",
                destination: frame.Source,
                correlation: frame.Correlation ?? frame.Id,
                summary: $"{firstError.Code}: {firstError.Message}");
            Write(nack);
        }
    }

    /// <inheritdoc />
    public void WriteStructured(QuackFrame frame) =>
        _logger.LogInformation("{StructuredTrace}", QuackTraceFormatter.FormatStructured(frame));
}
