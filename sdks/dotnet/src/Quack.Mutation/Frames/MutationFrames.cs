using System.Text.Json;

namespace Quack.Mutation;

public record EvidenceArtifact
{
    public string Kind { get; init; } = string.Empty;
    public string Digest { get; init; } = string.Empty;
    public string? Uri { get; init; }
    public string? MediaType { get; init; }
}

public sealed record SplashFrame : MutationFrameBase
{
    public required EvidenceArtifact[] EvidenceArtifacts { get; init; }

    public override QuackFrame ToQuackFrame()
    {
        var frame = BuildBase(QuackVerb.Splash);
        frame = frame with
        {
            Data = WriteData(w =>
            {
                w.WriteStartArray("evidenceArtifacts");
                foreach (var a in EvidenceArtifacts)
                {
                    w.WriteStartObject();
                    w.WriteString("kind", a.Kind);
                    w.WriteString("digest", a.Digest);
                    if (a.Uri is not null) w.WriteString("uri", a.Uri);
                    if (a.MediaType is not null) w.WriteString("mediaType", a.MediaType);
                    w.WriteEndObject();
                }
                w.WriteEndArray();
            }),
        };
        return frame;
    }

    public static SplashFrame FromQuackFrame(QuackFrame frame)
    {
        if (frame.Verb != QuackVerb.Splash)
            throw new InvalidOperationException($"Expected verb splash, got {frame.Verb}");

        var data = frame.Data ?? throw new InvalidOperationException("Splash frame requires data");
        var artifacts = data.GetProperty("evidenceArtifacts").EnumerateArray()
            .Select(a => new EvidenceArtifact
            {
                Kind = ReadStringProp(a, "kind"),
                Digest = ReadStringProp(a, "digest"),
                Uri = ReadOptionalStringProp(a, "uri"),
                MediaType = ReadOptionalStringProp(a, "mediaType"),
            }).ToArray();

        return new SplashFrame { EvidenceArtifacts = artifacts }.ApplyBase<SplashFrame>(frame);
    }
}

public sealed record EggFrame : MutationFrameBase
{
    public required string PlanId { get; init; }
    public required Dictionary<string, object> MutationIntent { get; init; }
    public required string IntentDigest { get; init; }
    public required Dictionary<string, object> ReviewSurface { get; init; }
    public required string ReviewDigest { get; init; }
    public required EvidenceArtifact[] EvidenceArtifacts { get; init; }
    public required string ApprovalPolicy { get; init; }
    public string ExecutionReusePolicy { get; init; } = "single-execution";
    public required string ValidFrom { get; init; }
    public required string ValidUntil { get; init; }
    public required Dictionary<string, object> FreshnessPolicy { get; init; }

    public override QuackFrame ToQuackFrame()
    {
        var frame = BuildBase(QuackVerb.Egg);
        frame = frame with
        {
            Digest = IntentDigest,
            Data = WriteData(w =>
            {
                w.WriteString("planId", PlanId);
                WriteDictionary(w, "mutationIntent", MutationIntent);
                w.WriteString("intentDigest", IntentDigest);
                WriteDictionary(w, "reviewSurface", ReviewSurface);
                w.WriteString("reviewDigest", ReviewDigest);
                WriteEvidenceArtifacts(w, EvidenceArtifacts);
                w.WriteString("approvalPolicy", ApprovalPolicy);
                w.WriteString("executionReusePolicy", ExecutionReusePolicy);
                w.WriteString("validFrom", ValidFrom);
                w.WriteString("validUntil", ValidUntil);
                WriteDictionary(w, "freshnessPolicy", FreshnessPolicy);
            }),
        };
        return frame;
    }

    public static EggFrame FromQuackFrame(QuackFrame frame)
    {
        if (frame.Verb != QuackVerb.Egg)
            throw new InvalidOperationException($"Expected verb egg, got {frame.Verb}");

        var data = frame.Data ?? throw new InvalidOperationException("Egg frame requires data");
        return new EggFrame
        {
            PlanId = data.GetProperty("planId").GetString()!,
            MutationIntent = ReadDictionary(data.GetProperty("mutationIntent")),
            IntentDigest = data.GetProperty("intentDigest").GetString()!,
            ReviewSurface = ReadDictionary(data.GetProperty("reviewSurface")),
            ReviewDigest = data.GetProperty("reviewDigest").GetString()!,
            EvidenceArtifacts = ReadEvidenceArtifacts(data.GetProperty("evidenceArtifacts")),
            ApprovalPolicy = data.GetProperty("approvalPolicy").GetString()!,
            ExecutionReusePolicy = data.GetProperty("executionReusePolicy").GetString()!,
            ValidFrom = data.GetProperty("validFrom").GetString()!,
            ValidUntil = data.GetProperty("validUntil").GetString()!,
            FreshnessPolicy = ReadDictionary(data.GetProperty("freshnessPolicy")),
        }.ApplyBase<EggFrame>(frame);
    }
}

public sealed record HatchFrame : MutationFrameBase
{
    public required string PlanId { get; init; }
    public required string ChallengeId { get; init; }
    public required string ApprovalUrl { get; init; }
    public required string IntentDigest { get; init; }
    public required string ReviewDigest { get; init; }
    public required string ChallengeExpiresAt { get; init; }

    public override QuackFrame ToQuackFrame()
    {
        var frame = BuildBase(QuackVerb.Hatch);
        frame = frame with
        {
            Data = WriteData(w =>
            {
                w.WriteString("planId", PlanId);
                w.WriteString("challengeId", ChallengeId);
                w.WriteString("approvalUrl", ApprovalUrl);
                w.WriteString("intentDigest", IntentDigest);
                w.WriteString("reviewDigest", ReviewDigest);
                w.WriteString("challengeExpiresAt", ChallengeExpiresAt);
            }),
        };
        return frame;
    }

    public static HatchFrame FromQuackFrame(QuackFrame frame)
    {
        if (frame.Verb != QuackVerb.Hatch)
            throw new InvalidOperationException($"Expected verb hatch, got {frame.Verb}");

        var data = frame.Data ?? throw new InvalidOperationException("Hatch frame requires data");
        return new HatchFrame
        {
            PlanId = data.GetProperty("planId").GetString()!,
            ChallengeId = data.GetProperty("challengeId").GetString()!,
            ApprovalUrl = data.GetProperty("approvalUrl").GetString()!,
            IntentDigest = data.GetProperty("intentDigest").GetString()!,
            ReviewDigest = data.GetProperty("reviewDigest").GetString()!,
            ChallengeExpiresAt = data.GetProperty("challengeExpiresAt").GetString()!,
        }.ApplyBase<HatchFrame>(frame);
    }
}

public sealed record FlapFrame : MutationFrameBase
{
    public required string PlanId { get; init; }
    public required string GrantId { get; init; }
    public required string IntentDigest { get; init; }
    public required string ReviewDigest { get; init; }
    public required string[] PreExecutionGates { get; init; }

    public override QuackFrame ToQuackFrame()
    {
        var frame = BuildBase(QuackVerb.Flap);
        frame = frame with
        {
            Digest = IntentDigest,
            Data = WriteData(w =>
            {
                w.WriteString("planId", PlanId);
                w.WriteString("grantId", GrantId);
                w.WriteString("intentDigest", IntentDigest);
                w.WriteString("reviewDigest", ReviewDigest);
                w.WriteStartArray("preExecutionGates");
                foreach (var gate in PreExecutionGates)
                    w.WriteStringValue(gate);
                w.WriteEndArray();
            }),
        };
        return frame;
    }

    public static FlapFrame FromQuackFrame(QuackFrame frame)
    {
        if (frame.Verb != QuackVerb.Flap)
            throw new InvalidOperationException($"Expected verb flap, got {frame.Verb}");

        var data = frame.Data ?? throw new InvalidOperationException("Flap frame requires data");
        return new FlapFrame
        {
            PlanId = data.GetProperty("planId").GetString()!,
            GrantId = data.GetProperty("grantId").GetString()!,
            IntentDigest = data.GetProperty("intentDigest").GetString()!,
            ReviewDigest = data.GetProperty("reviewDigest").GetString()!,
            PreExecutionGates = data.GetProperty("preExecutionGates").EnumerateArray()
                .Select(e => e.GetString()!).ToArray(),
        }.ApplyBase<FlapFrame>(frame);
    }
}

public sealed record PerchFrame : MutationFrameBase
{
    public required string PlanId { get; init; }
    public required string ExecutionId { get; init; }
    public required string Outcome { get; init; }
    public required string ObservedAt { get; init; }

    public override QuackFrame ToQuackFrame()
    {
        var frame = BuildBase(QuackVerb.Perch);
        frame = frame with
        {
            Data = WriteData(w =>
            {
                w.WriteString("planId", PlanId);
                w.WriteString("executionId", ExecutionId);
                w.WriteString("outcome", Outcome);
                w.WriteString("observedAt", ObservedAt);
            }),
        };
        return frame;
    }

    public static PerchFrame FromQuackFrame(QuackFrame frame)
    {
        if (frame.Verb != QuackVerb.Perch)
            throw new InvalidOperationException($"Expected verb perch, got {frame.Verb}");

        var data = frame.Data ?? throw new InvalidOperationException("Perch frame requires data");
        return new PerchFrame
        {
            PlanId = data.GetProperty("planId").GetString()!,
            ExecutionId = data.GetProperty("executionId").GetString()!,
            Outcome = data.GetProperty("outcome").GetString()!,
            ObservedAt = data.GetProperty("observedAt").GetString()!,
        }.ApplyBase<PerchFrame>(frame);
    }
}

public sealed record HonkFrame : MutationFrameBase
{
    public required string ReasonCode { get; init; }
    public required string Reason { get; init; }
    public string? PlanId { get; init; }
    public string? ChallengeId { get; init; }
    public string? GrantId { get; init; }
    public string? ExecutionId { get; init; }
    public string? FailedGate { get; init; }
    public EvidenceArtifact[]? EvidenceArtifacts { get; init; }

    public override QuackFrame ToQuackFrame()
    {
        var frame = BuildBase(QuackVerb.Honk);
        frame = frame with
        {
            Data = WriteData(w =>
            {
                w.WriteString("reasonCode", ReasonCode);
                w.WriteString("reason", Reason);
                if (PlanId is not null) w.WriteString("planId", PlanId);
                if (ChallengeId is not null) w.WriteString("challengeId", ChallengeId);
                if (GrantId is not null) w.WriteString("grantId", GrantId);
                if (ExecutionId is not null) w.WriteString("executionId", ExecutionId);
                if (FailedGate is not null) w.WriteString("failedGate", FailedGate);
                if (EvidenceArtifacts is not null) WriteEvidenceArtifacts(w, EvidenceArtifacts);
            }),
        };
        return frame;
    }

    public static HonkFrame FromQuackFrame(QuackFrame frame)
    {
        if (frame.Verb != QuackVerb.Honk)
            throw new InvalidOperationException($"Expected verb honk, got {frame.Verb}");

        var data = frame.Data ?? throw new InvalidOperationException("Honk frame requires data");
        return new HonkFrame
        {
            ReasonCode = data.GetProperty("reasonCode").GetString()!,
            Reason = data.GetProperty("reason").GetString()!,
            PlanId = ReadOptionalStringProp(data, "planId"),
            ChallengeId = ReadOptionalStringProp(data, "challengeId"),
            GrantId = ReadOptionalStringProp(data, "grantId"),
            ExecutionId = ReadOptionalStringProp(data, "executionId"),
            FailedGate = ReadOptionalStringProp(data, "failedGate"),
            EvidenceArtifacts = data.TryGetProperty("evidenceArtifacts", out var arr)
                ? ReadEvidenceArtifacts(arr) : null,
        }.ApplyBase<HonkFrame>(frame);
    }
}

public sealed record MoltFrame : MutationFrameBase
{
    public required string TerminalFor { get; init; }
    public required string ReasonCode { get; init; }
    public required string Reason { get; init; }
    public string? PlanId { get; init; }
    public string? ChallengeId { get; init; }
    public string? SupersededByPlanId { get; init; }
    public string? SupersededByFrameId { get; init; }

    public override QuackFrame ToQuackFrame()
    {
        var frame = BuildBase(QuackVerb.Molt);
        frame = frame with
        {
            Data = WriteData(w =>
            {
                w.WriteString("terminalFor", TerminalFor);
                w.WriteString("reasonCode", ReasonCode);
                w.WriteString("reason", Reason);
                if (PlanId is not null) w.WriteString("planId", PlanId);
                if (ChallengeId is not null) w.WriteString("challengeId", ChallengeId);
                if (SupersededByPlanId is not null) w.WriteString("supersededByPlanId", SupersededByPlanId);
                if (SupersededByFrameId is not null) w.WriteString("supersededByFrameId", SupersededByFrameId);
            }),
        };
        return frame;
    }

    public static MoltFrame FromQuackFrame(QuackFrame frame)
    {
        if (frame.Verb != QuackVerb.Molt)
            throw new InvalidOperationException($"Expected verb molt, got {frame.Verb}");

        var data = frame.Data ?? throw new InvalidOperationException("Molt frame requires data");
        return new MoltFrame
        {
            TerminalFor = data.GetProperty("terminalFor").GetString()!,
            ReasonCode = data.GetProperty("reasonCode").GetString()!,
            Reason = data.GetProperty("reason").GetString()!,
            PlanId = ReadOptionalStringProp(data, "planId"),
            ChallengeId = ReadOptionalStringProp(data, "challengeId"),
            SupersededByPlanId = ReadOptionalStringProp(data, "supersededByPlanId"),
            SupersededByFrameId = ReadOptionalStringProp(data, "supersededByFrameId"),
        }.ApplyBase<MoltFrame>(frame);
    }
}
