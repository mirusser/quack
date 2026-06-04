using System.Text.RegularExpressions;

namespace Quack;

/// <summary>
/// Digest binding utilities per spec adapters §8.
/// Validates digest format: &lt;algorithm&gt;:&lt;hex-encoded digest&gt;.
/// Supported algorithms: sha256 (64 hex chars), sha512 (128 hex chars).
/// </summary>
public static class QuackDigest
{
    private static readonly Regex Sha256Pattern =
        new(@"^sha256:[0-9a-f]{64}$", RegexOptions.Compiled);

    private static readonly Regex Sha512Pattern =
        new(@"^sha512:[0-9a-f]{128}$", RegexOptions.Compiled);

    /// <summary>
    /// Try to parse a digest string into algorithm and hex components.
    /// Returns true when the format is valid.
    /// </summary>
    public static bool TryParse(string digest, out string algorithm, out string hexDigest)
    {
        algorithm = string.Empty;
        hexDigest = string.Empty;

        if (string.IsNullOrEmpty(digest))
            return false;

        var colonIdx = digest.IndexOf(':');
        if (colonIdx <= 0)
            return false;

        algorithm = digest[..colonIdx];
        hexDigest = digest[(colonIdx + 1)..];
        return true;
    }

    /// <summary>Validate that a digest string matches a supported format (sha256 or sha512).</summary>
    public static bool IsValid(string digest) =>
        !string.IsNullOrEmpty(digest) &&
        (Sha256Pattern.IsMatch(digest) || Sha512Pattern.IsMatch(digest));
}
