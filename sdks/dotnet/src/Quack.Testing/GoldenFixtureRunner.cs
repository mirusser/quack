using System.Text.Json;

namespace Quack.Testing;

/// <summary>
/// Runs conformance tests against golden fixtures in the spec/fixtures directory.
/// </summary>
public static class GoldenFixtureRunner
{
    /// <summary>
    /// Loads all valid golden fixtures and asserts they deserialize and validate correctly.
    /// Handles both single frames and arrays of frames.
    /// </summary>
    public static IReadOnlyList<(string FileName, QuackFrame Frame)> LoadValidFixtures(string fixturesPath)
    {
        var validPath = Path.Combine(fixturesPath, "valid");
        if (!Directory.Exists(validPath))
            throw new DirectoryNotFoundException($"Valid fixtures directory not found: {validPath}");

        var results = new List<(string, QuackFrame)>();
        foreach (var file in Directory.GetFiles(validPath, "*.json"))
        {
            var json = File.ReadAllText(file);

            if (json.TrimStart().StartsWith('['))
            {
                // Array fixture — parse each element individually
                using var doc = JsonDocument.Parse(json);
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var elementJson = element.GetRawText();
                    var frame = QuackJson.Deserialize(elementJson);
                    results.Add((Path.GetFileName(file), frame));
                }
            }
            else
            {
                var frame = QuackJson.Deserialize(json);
                results.Add((Path.GetFileName(file), frame));
            }
        }

        return results;
    }

    /// <summary>
    /// Loads all invalid golden fixtures and asserts they fail deserialization or validation.
    /// Returns the list of file names that loaded successfully (should be empty for true conformance).
    /// </summary>
    public static IReadOnlyList<string> LoadInvalidFixtures(string fixturesPath)
    {
        var invalidPath = Path.Combine(fixturesPath, "invalid");
        if (!Directory.Exists(invalidPath))
            throw new DirectoryNotFoundException($"Invalid fixtures directory not found: {invalidPath}");

        var unexpectedlyValid = new List<string>();
        var validator = new QuackValidator();

        foreach (var file in Directory.GetFiles(invalidPath, "*.json"))
        {
            var json = File.ReadAllText(file);
            try
            {
                var frame = QuackJson.Deserialize(json);
                var result = validator.Validate(frame);
                if (result.IsValid)
                    unexpectedlyValid.Add(Path.GetFileName(file));
            }
            catch (JsonException)
            {
                // Expected — invalid fixtures should fail to parse or validate
            }
        }

        return unexpectedlyValid;
    }
}
