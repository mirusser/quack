namespace Quack.A2A;

/// <summary>
/// Extension methods for adding Quack capability to an A2A Agent Card.
/// </summary>
public static class QuackAgentCardExtensions
{
    /// <summary>
    /// Registers Quack as an A2A extension capability on the agent card.
    /// </summary>
    public static IDictionary<string, object> CreateQuackExtension(QuackOptions options)
    {
        return new Dictionary<string, object>
        {
            ["uri"] = "https://example.org/ext/quack/v0.1",
            ["description"] = "Quack semantic protocol for agent coordination.",
            ["required"] = false,
            ["params"] = new Dictionary<string, object>
            {
                ["maxRisk"] = SerializeRisk(options.MaxRisk),
                ["maxTtl"] = options.MaxTtl ?? 0,
                ["maxQuackVersion"] = options.MaxQuackVersion,
                ["supportedEncodings"] = (string[])
                [
                    "application/vnd.quack+json",
                    "text/vnd.quack",
                ],
            },
        };
    }

    private static string SerializeRisk(QuackRisk risk) => risk switch
    {
        QuackRisk.None => "none",
        QuackRisk.Low => "low",
        QuackRisk.Medium => "medium",
        QuackRisk.High => "high",
        QuackRisk.Critical => "critical",
        _ => "none",
    };
}
