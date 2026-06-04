using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Quack.Mutation;

/// <summary>
/// Mutation-profile digest utilities. Extends Quack.QuackDigest with SHA-256 computation
/// and JCS (RFC 8785) canonicalization per spec §5.
/// </summary>
public static class QuackDigest
{
    private const string Prefix = "sha256:";
    private const int HashHexLength = 64;

    /// <summary>Validate that a string matches a supported digest format. Delegates to Quack.QuackDigest.</summary>
    public static bool ValidateFormat(string digest) => global::Quack.QuackDigest.IsValid(digest);

    /// <summary>Compute a digest over a JSON string using JCS canonicalization + SHA-256.</summary>
    public static string Compute(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var canonical = JsonCanonicalize(json);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Prefix + Convert.ToHexStringLower(hash);
    }

    /// <summary>Canonicalize a JSON string per JCS (RFC 8785) — sort keys, strip whitespace.</summary>
    public static string JsonCanonicalize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        using var doc = JsonDocument.Parse(json);
        return WriteCanonical(doc.RootElement);
    }

    private static string WriteCanonical(JsonElement element)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });
        WriteElementCanonical(writer, element);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteElementCanonical(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                var properties = element.EnumerateObject()
                    .OrderBy(p => p.Name, StringComparer.Ordinal)
                    .ToList();
                foreach (var prop in properties)
                {
                    writer.WritePropertyName(prop.Name);
                    WriteElementCanonical(writer, prop.Value);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteElementCanonical(writer, item);
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText());
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
        }
    }

}
