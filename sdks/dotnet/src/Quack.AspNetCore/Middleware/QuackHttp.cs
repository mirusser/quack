using System.Text;

namespace Quack;

/// <summary>
/// Quack-HTTP Structured Field encoding for HTTP headers.
/// Produces RFC 9651 Dictionary syntax per protocol spec §5.5.
/// </summary>
public static class QuackHttp
{
    /// <summary>Encode a frame as an RFC 9651 Dictionary header value.</summary>
    public static string EncodeHeader(QuackFrame frame)
    {
        var sb = new StringBuilder();
        sb.Append($"version={frame.Version}");
        sb.Append($", verb=\"{frame.Verb.ToString().ToLowerInvariant()}\"");
        sb.Append($", id=\"{frame.Id}\"");
        sb.Append($", source=\"{frame.Source}\"");

        if (frame.Destination is { } dst)
            sb.Append($", destination=\"{dst}\"");
        if (frame.Context is { } ctx)
            sb.Append($", context=\"{ctx}\"");
        if (frame.Correlation is { } corr)
            sb.Append($", correlation=\"{corr}\"");
        if (frame.Summary is { } say)
            sb.Append($", summary=\"{EscapeHeaderValue(say)}\"");

        sb.Append($", risk={SerializeRisk(frame.Risk)}");

        return sb.ToString();
    }

    /// <summary>Encode trace context as an RFC 9651 Dictionary value.</summary>
    public static string EncodeTraceHeader(QuackFrame frame)
    {
        var sb = new StringBuilder();
        if (frame.Context is { } ctx)
            sb.Append($"context=\"{ctx}\"");
        if (frame.Correlation is { } corr)
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append($"correlation=\"{corr}\"");
        }
        return sb.ToString();
    }

    /// <summary>Decode an RFC 9651 Dictionary header value to a frame.</summary>
    public static QuackFrame DecodeHeader(string headerValue)
    {
        var frame = new QuackFrame();
        var pairs = ParseDictionary(headerValue);

        foreach (var (key, value) in pairs)
        {
            switch (key.ToLowerInvariant())
            {
                case "version": frame = frame with { Version = int.TryParse(value, out var v) ? v : 0 }; break;
                case "verb": frame = frame with { Verb = ParseVerb(value) }; break;
                case "id": frame = frame with { Id = value }; break;
                case "source": frame = frame with { Source = value }; break;
                case "destination": frame = frame with { Destination = value }; break;
                case "context": frame = frame with { Context = value }; break;
                case "correlation": frame = frame with { Correlation = value }; break;
                case "summary": frame = frame with { Summary = value }; break;
                case "risk": frame = frame with { Risk = ParseRisk(value) }; break;
            }
        }

        return frame;
    }

    private static List<(string Key, string Value)> ParseDictionary(string input)
    {
        var result = new List<(string, string)>();
        var inQuotes = false;
        var currentToken = new StringBuilder();
        string? currentKey = null;

        for (int i = 0; i < input.Length; i++)
        {
            var ch = input[i];

            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && ch == ',')
            {
                if (currentKey is not null)
                    result.Add((currentKey, currentToken.ToString().Trim()));
                currentKey = null;
                currentToken.Clear();
                continue;
            }

            if (!inQuotes && ch == '=')
            {
                currentKey = currentToken.ToString().Trim();
                currentToken.Clear();
                continue;
            }

            currentToken.Append(ch);
        }

        if (currentKey is not null)
            result.Add((currentKey, currentToken.ToString().Trim()));

        return result;
    }

    private static string SerializeRisk(QuackRisk risk) => risk switch
    {
        QuackRisk.None => "n",
        QuackRisk.Low => "l",
        QuackRisk.Medium => "m",
        QuackRisk.High => "h",
        QuackRisk.Critical => "c",
        _ => "n",
    };

    private static QuackVerb ParseVerb(string value) => value.ToLowerInvariant() switch
    {
        "quack" => QuackVerb.Quack,
        "peck" => QuackVerb.Peck,
        "bob" => QuackVerb.Bob,
        "nack" => QuackVerb.Nack,
        "egg" => QuackVerb.Egg,
        "hatch" => QuackVerb.Hatch,
        "flap" => QuackVerb.Flap,
        "perch" => QuackVerb.Perch,
        "honk" => QuackVerb.Honk,
        "molt" => QuackVerb.Molt,
        "splash" => QuackVerb.Splash,
        _ => QuackVerb.Quack,
    };

    private static QuackRisk ParseRisk(string value) => value.ToLowerInvariant() switch
    {
        "n" => QuackRisk.None,
        "l" => QuackRisk.Low,
        "m" => QuackRisk.Medium,
        "h" => QuackRisk.High,
        "c" => QuackRisk.Critical,
        _ => QuackRisk.None,
    };

    private static string EscapeHeaderValue(string value)
    {
        if (value.AsSpan().IndexOfAny('"', '\\') < 0)
            return value;
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
