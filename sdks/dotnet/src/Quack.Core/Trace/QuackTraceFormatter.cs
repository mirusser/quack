namespace Quack;

public static class QuackTraceFormatter
{
    private static readonly Dictionary<QuackVerb, string> Emoji = new()
    {
        [QuackVerb.Quack] = "🦆",
        [QuackVerb.Peck] = "🐤",
        [QuackVerb.Bob] = "🦢",
        [QuackVerb.Nack] = "🐦‍⬛",
        [QuackVerb.Egg] = "🥚",
        [QuackVerb.Hatch] = "🐣",
        [QuackVerb.Flap] = "🪽",
        [QuackVerb.Perch] = "🕊️",
        [QuackVerb.Honk] = "🪿",
        [QuackVerb.Molt] = "🪹",
        [QuackVerb.Splash] = "💦",
        [QuackVerb.Dabble] = "🔍",
        [QuackVerb.Preen] = "🪶",
        [QuackVerb.Settle] = "✅",
        [QuackVerb.Shun] = "🚫",
    };

    /// <summary>Render a frame as an emoji-annotated trace line.</summary>
    public static string FormatEmoji(QuackFrame frame)
    {
        var emoji = Emoji.GetValueOrDefault(frame.Verb, "❓");
        var text = QuackText.Encode(frame);
        return $"{emoji} {text}";
    }

    /// <summary>
    /// Render a frame as a structured trace line per spec adapters §6.1.
    /// Fixed-width columns: timestamp(24) verb(8) id(26) src(12) dst(12) ctx(24) corr(16) risk(1) digest(20) summary.
    /// </summary>
    public static string FormatStructured(QuackFrame frame)
    {
        var ts = (frame.Timestamp ?? "-").PadRight(24)[..24];
        var verb = frame.Verb.ToString().ToLowerInvariant().PadRight(8)[..8];
        var id = (frame.Id ?? "-").PadRight(26)[..26];
        var src = Truncate(frame.Source, 12);
        var dst = Truncate(frame.Destination ?? "-", 12);
        var ctx = Truncate(frame.Context ?? "-", 24);
        var corr = Truncate(frame.Correlation ?? "-", 16);
        var risk = RiskChar(frame.Risk);
        var digest = Truncate(frame.Digest ?? "-", 20);
        var summary = frame.Summary ?? string.Empty;

        return $"{ts} {verb} {id} {src} {dst} {ctx} {corr} {risk} {digest} {summary}";
    }

    private static string Truncate(string value, int width)
    {
        if (value.Length <= width) return value.PadRight(width);
        return string.Concat(value.AsSpan(0, width - 1), "…");
    }

    private static char RiskChar(QuackRisk risk) => risk switch
    {
        QuackRisk.None => 'n',
        QuackRisk.Low => 'l',
        QuackRisk.Medium => 'm',
        QuackRisk.High => 'h',
        QuackRisk.Critical => 'c',
        _ => '-',
    };
}
