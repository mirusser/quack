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
        sb.Append(" source=");
        sb.Append(EncodeValue(frame.Source));

        if (frame.Destination is { } dst)
            sb.Append(" destination=").Append(EncodeValue(dst));
        if (frame.Context is { } ctx)
            sb.Append(" context=").Append(EncodeValue(ctx));
        if (frame.Correlation is { } corr)
            sb.Append(" correlation=").Append(EncodeValue(corr));
        if (frame.Timestamp is { } ts)
            sb.Append(" timestamp=").Append(EncodeValue(ts));
        sb.Append(" risk=").Append(FormatRisk(frame.Risk));
        if (frame.Summary is { } summary)
            sb.Append(" summary=").Append(EncodeValue(summary));
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

    private static string FormatRisk(QuackRisk risk) => risk switch
    {
        QuackRisk.None => "none",
        QuackRisk.Low => "low",
        QuackRisk.Medium => "medium",
        QuackRisk.High => "high",
        QuackRisk.Critical => "critical",
        _ => risk.ToString().ToLowerInvariant(),
    };

    /// <summary>Decode a Quack-Text line back to a frame.</summary>
    public static QuackFrame Decode(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);

        var span = text.AsSpan().Trim();
        if (!span.StartsWith("QK1 "))
            throw new FormatException("Quack-Text must start with 'QK1 '");

        span = span[4..];

        var verbEnd = span.IndexOf(' ');
        var verbStr = verbEnd < 0 ? span.ToString() : span[..verbEnd].ToString();
        if (!Enum.TryParse<QuackVerb>(verbStr, ignoreCase: true, out var verb))
            throw new FormatException($"Unknown Quack verb: '{verbStr}'");

        span = verbEnd < 0 ? ReadOnlySpan<char>.Empty : span[(verbEnd + 1)..];

        var frame = new QuackFrame
        {
            Version = "0.1",
            Verb = verb,
        };

        while (!span.IsEmpty)
        {
            span = span.TrimStart();
            if (span.IsEmpty)
                break;

            var eqIdx = span.IndexOf('=');
            if (eqIdx < 0)
                throw new FormatException("Expected key=value pair");

            var key = span[..eqIdx].ToString();
            span = span[(eqIdx + 1)..];

            var value = ReadValue(ref span);
            frame = ApplyField(frame, key, value);
        }

        return frame;
    }

    private static string ReadValue(ref ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
            return string.Empty;

        if (span[0] == '"')
        {
            span = span[1..];
            var sb = new System.Text.StringBuilder();
            while (!span.IsEmpty)
            {
                if (span[0] == '\\' && span.Length > 1)
                {
                    sb.Append(span[1]);
                    span = span[2..];
                }
                else if (span[0] == '"')
                {
                    span = span[1..];
                    break;
                }
                else
                {
                    sb.Append(span[0]);
                    span = span[1..];
                }
            }
            return sb.ToString();
        }

        var end = 0;
        while (end < span.Length && span[end] != ' ')
            end++;
        var value = span[..end].ToString();
        span = end < span.Length ? span[(end + 1)..] : ReadOnlySpan<char>.Empty;
        return value;
    }

    private static QuackFrame ApplyField(QuackFrame frame, string key, string value)
    {
        switch (key)
        {
            case "quackId":
                frame = frame with { Id = value };
                break;
            case "timestamp":
                frame = frame with { Timestamp = value };
                break;
            case "source":
                frame = frame with { Source = value };
                break;
            case "destination":
                frame = frame with { Destination = value };
                break;
            case "context":
                frame = frame with { Context = value };
                break;
            case "correlation":
                frame = frame with { Correlation = value };
                break;
            case "risk":
                frame = frame with { Risk = ParseRisk(value) };
                break;
            case "summary":
                frame = frame with { Summary = value };
                break;
            case "digest":
                frame = frame with { Digest = value };
                break;
            case "ttl":
                if (int.TryParse(value, out var ttl))
                    frame = frame with { Ttl = ttl };
                break;
            case "tone":
                frame = frame with { Tone = ParseTone(value) };
                break;
        }
        return frame;
    }

    private static QuackRisk ParseRisk(string value) => value.ToLowerInvariant() switch
    {
        "none" => QuackRisk.None,
        "low" => QuackRisk.Low,
        "medium" => QuackRisk.Medium,
        "high" => QuackRisk.High,
        "critical" => QuackRisk.Critical,
        _ => QuackRisk.None,
    };

    private static QuackTone ParseTone(string value) => value.ToLowerInvariant() switch
    {
        "serious-duck" => QuackTone.SeriousDuck,
        "playful-duck" => QuackTone.PlayfulDuck,
        "angry-goose" => QuackTone.AngryGoose,
        "sleepy-duckling" => QuackTone.SleepyDuckling,
        _ => QuackTone.SeriousDuck,
    };
}
