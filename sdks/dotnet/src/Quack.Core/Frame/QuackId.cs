using System.Buffers.Text;
using System.Text;

namespace Quack;

/// <summary>
/// Generates monotonic ULID-compatible frame IDs.
/// 26 characters, URL-safe Crockford Base32, sortable.
/// </summary>
public static class QuackId
{
    // Crockford's Base32 alphabet
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private static readonly Random _random = new();
    private static long _lastTicks = -1;
    private static readonly object _lock = new();

    /// <summary>
    /// Returns a new 26-character ULID string.
    /// Monotonically increasing within a process.
    /// </summary>
    public static string NewId()
    {
        // 10 chars for timestamp (ms precision, 48 bits)
        // 16 chars for randomness (80 bits)
        Span<byte> bytes = stackalloc byte[16];
        Span<char> chars = stackalloc char[26];

        long ticks;
        lock (_lock)
        {
            ticks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (ticks <= _lastTicks)
                ticks = _lastTicks + 1;
            _lastTicks = ticks;
        }

        // Encode timestamp (48 bits → 10 base32 chars)
        for (int i = 9; i >= 0; i--)
        {
            chars[i] = Alphabet[(int)(ticks & 0x1F)];
            ticks >>= 5;
        }

        // Encode randomness (80 bits → 16 base32 chars)
        lock (_lock)
        {
            _random.NextBytes(bytes);
        }

        int bitBuffer = 0;
        int bitsRemaining = 0;
        int charIndex = 10;

        for (int i = 0; i < 16; i++)
        {
            bitBuffer = (bitBuffer << 8) | bytes[i];
            bitsRemaining += 8;

            while (bitsRemaining >= 5 && charIndex < 26)
            {
                bitsRemaining -= 5;
                chars[charIndex++] = Alphabet[(bitBuffer >> bitsRemaining) & 0x1F];
            }
        }

        // Flush remaining bits
        if (bitsRemaining > 0 && charIndex < 26)
        {
            chars[charIndex++] = Alphabet[(bitBuffer << (5 - bitsRemaining)) & 0x1F];
        }

        return new string(chars);
    }
}
