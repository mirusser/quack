using System.Text.Json;

namespace Quack.Mutation;

public static class MutationA2AExtensions
{
    public const string MutationMediaType = "application/vnd.quack+json";
    public const string MutationProfile = "quack-mutation-v0";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static A2AMutationPart ToA2APart(this MutationFrameBase frame)
    {
        var quackFrame = frame.ToQuackFrame();
        var json = JsonSerializer.Serialize(quackFrame, SerializerOptions);

        using var doc = JsonDocument.Parse(json);
        return new A2AMutationPart
        {
            MediaType = MutationMediaType,
            Data = doc.RootElement.Clone(),
            Verb = quackFrame.Verb.ToString().ToLowerInvariant(),
        };
    }

    public static MutationFrameBase FromA2APart(A2AMutationPart part)
    {
        if (part.MediaType != MutationMediaType)
            throw new InvalidOperationException($"Expected media type {MutationMediaType}, got {part.MediaType}");

        var json = JsonSerializer.Serialize(part.Data, SerializerOptions);
        var frame = JsonSerializer.Deserialize<QuackFrame>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Failed to deserialize A2A part as QuackFrame");

        return frame.Verb switch
        {
            QuackVerb.Splash => SplashFrame.FromQuackFrame(frame),
            QuackVerb.Egg => EggFrame.FromQuackFrame(frame),
            QuackVerb.Hatch => HatchFrame.FromQuackFrame(frame),
            QuackVerb.Flap => FlapFrame.FromQuackFrame(frame),
            QuackVerb.Perch => PerchFrame.FromQuackFrame(frame),
            QuackVerb.Honk => HonkFrame.FromQuackFrame(frame),
            QuackVerb.Molt => MoltFrame.FromQuackFrame(frame),
            _ => throw new InvalidOperationException($"Unsupported mutation verb: {frame.Verb}"),
        };
    }
}

public sealed record A2AMutationPart
{
    public string MediaType { get; init; } = MutationA2AExtensions.MutationMediaType;
    public JsonElement Data { get; init; }
    public string Verb { get; init; } = string.Empty;
    public string? Context { get; init; }
    public string? TaskId { get; init; }

    public void ValidateContainerConsistency(QuackFrame frame)
    {
        if (Context is not null && frame.Context is not null && Context != frame.Context)
            throw new InvalidOperationException(
                $"Container context '{Context}' does not match frame context '{frame.Context}'");
        // TaskId match is domain-specific and not enforced at protocol level
    }
}
