namespace Quack;

/// <summary>
/// Quack-Text encoding: human-readable single-line format.
/// Produces "QK1 verb key=value*" lines.
/// </summary>
public static class QuackText
{
    /// <summary>Encode a frame to a Quack-Text line.</summary>
    public static string Encode(QuackFrame frame)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("QK1 ");
        sb.Append(frame.Verb.ToString().ToLowerInvariant());
        sb.Append(" quackId=");
        sb.Append(frame.Id);
        sb.Append(" src=");
        sb.Append(EncodeValue(frame.Source));

        if (frame.Destination is { } dst)
            sb.Append(" destination=").Append(EncodeValue(dst));
        if (frame.Context is { } ctx)
            sb.Append(" context=").Append(EncodeValue(ctx));
        if (frame.Correlation is { } corr)
            sb.Append(" correlation=").Append(EncodeValue(corr));
        if (frame.Summary is { } summary)
            sb.Append(" say=").Append(EncodeValue(summary));
        if (frame.Digest is { } digest)
            sb.Append(" digest=").Append(digest);
        if (frame.Ttl is { } ttl)
            sb.Append(" ttl=").Append(ttl);
        if (frame.Tone is { } tone)
            sb.Append(" tone=").Append(FormatTone(tone));

        return sb.ToString();
    }

    /// <summary>Encode a value, quoting if needed.</summary>
    public static string EncodeValue(string value)
    {
        bool needsQuoting = value.AsSpan().IndexOfAny(' ', '"', '=') >= 0;
        if (!needsQuoting)
            return value;

        var escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }

    private static string FormatTone(QuackTone tone) => tone switch
    {
        QuackTone.SeriousDuck => "serious-duck",
        QuackTone.PlayfulDuck => "playful-duck",
        QuackTone.AngryGoose => "angry-goose",
        QuackTone.SleepyDuckling => "sleepy-duckling",
        _ => tone.ToString().ToLowerInvariant(),
    };
}
