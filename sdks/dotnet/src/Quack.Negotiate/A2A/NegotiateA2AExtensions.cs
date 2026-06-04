using System.Text.Json;

namespace Quack.Negotiate;

public static class NegotiateA2AExtensions
{
    public const string NegotiateMediaType = "application/vnd.quack+json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static A2ANegotiatePart ToA2APart(this NegotiateFrameBase frame)
    {
        var quackFrame = frame.ToQuackFrame();
        var json = JsonSerializer.Serialize(quackFrame, SerializerOptions);

        using var doc = JsonDocument.Parse(json);
        return new A2ANegotiatePart
        {
            MediaType = NegotiateMediaType,
            Data = doc.RootElement.Clone(),
            Verb = quackFrame.Verb.ToString().ToLowerInvariant(),
        };
    }

    public static NegotiateFrameBase FromA2APart(A2ANegotiatePart part)
    {
        if (part.MediaType != NegotiateMediaType)
            throw new InvalidOperationException($"Expected media type {NegotiateMediaType}, got {part.MediaType}");

        var json = JsonSerializer.Serialize(part.Data, SerializerOptions);
        var frame = JsonSerializer.Deserialize<QuackFrame>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Failed to deserialize A2A part as QuackFrame");

        return frame.Verb switch
        {
            QuackVerb.Dabble => DabbleFrame.FromQuackFrame(frame),
            QuackVerb.Preen => PreenFrame.FromQuackFrame(frame),
            QuackVerb.Settle => SettleFrame.FromQuackFrame(frame),
            QuackVerb.Shun => ShunFrame.FromQuackFrame(frame),
            _ => throw new InvalidOperationException($"Unsupported negotiate verb: {frame.Verb}"),
        };
    }
}

public sealed record A2ANegotiatePart
{
    public string MediaType { get; init; } = NegotiateA2AExtensions.NegotiateMediaType;
    public JsonElement Data { get; init; }
    public string Verb { get; init; } = string.Empty;
    public string? Context { get; init; }
    public string? TaskId { get; init; }

    public void ValidateContainerConsistency(QuackFrame frame)
    {
        if (Context is not null && frame.Context is not null && Context != frame.Context)
            throw new InvalidOperationException(
                $"Container context '{Context}' does not match frame context '{frame.Context}'");
    }
}
