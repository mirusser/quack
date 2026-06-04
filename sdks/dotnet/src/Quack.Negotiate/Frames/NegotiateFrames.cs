using System.Text.Json;

namespace Quack.Negotiate;

public sealed record DabbleFrame : NegotiateFrameBase
{
    public required string TaskIntent { get; init; }
    public required string[] RequiredCapabilities { get; init; }
    public Dictionary<string, object> Constraints { get; init; } = new();
    public string[] PreferredSkills { get; init; } = [];

    public override QuackFrame ToQuackFrame()
    {
        var frame = BuildBase(QuackVerb.Dabble);
        frame = frame with
        {
            Data = WriteData(w =>
            {
                w.WriteString("taskIntent", TaskIntent);
                WriteStringArray(w, "requiredCapabilities", RequiredCapabilities);
                if (Constraints.Count > 0) WriteDict(w, "constraints", Constraints);
                if (PreferredSkills.Length > 0) WriteStringArray(w, "preferredSkills", PreferredSkills);
            }),
        };
        return frame;
    }

    public static DabbleFrame FromQuackFrame(QuackFrame frame)
    {
        if (frame.Verb != QuackVerb.Dabble)
            throw new InvalidOperationException($"Expected verb dabble, got {frame.Verb}");
        var data = frame.Data ?? throw new InvalidOperationException("Dabble frame requires data");
        return new DabbleFrame
        {
            TaskIntent = data.GetProperty("taskIntent").GetString()!,
            RequiredCapabilities = ReadStringArray(data.GetProperty("requiredCapabilities")),
            Constraints = data.TryGetProperty("constraints", out var c) ? ReadDict(c) : new(),
            PreferredSkills = data.TryGetProperty("preferredSkills", out var p) ? ReadStringArray(p) : [],
        }.ApplyBase<DabbleFrame>(frame);
    }
}

public sealed record PreenFrame : NegotiateFrameBase
{
    public required string SkillId { get; init; }
    public required string Applicability { get; init; }
    public required Dictionary<string, object> Constraints { get; init; }
    public required string AssuranceLevel { get; init; }
    public string? ApplicabilityNotes { get; init; }
    public string[] PreExecutionGates { get; init; } = [];
    public string? EstimatedDuration { get; init; }
    public Dictionary<string, object>? InputSchema { get; init; }

    public override QuackFrame ToQuackFrame()
    {
        var frame = BuildBase(QuackVerb.Preen);
        frame = frame with
        {
            Data = WriteData(w =>
            {
                w.WriteString("skillId", SkillId);
                w.WriteString("applicability", Applicability);
                WriteDict(w, "constraints", Constraints);
                w.WriteString("assuranceLevel", AssuranceLevel);
                if (ApplicabilityNotes is not null) w.WriteString("applicabilityNotes", ApplicabilityNotes);
                if (PreExecutionGates.Length > 0) WriteStringArray(w, "preExecutionGates", PreExecutionGates);
                if (EstimatedDuration is not null) w.WriteString("estimatedDuration", EstimatedDuration);
                if (InputSchema is not null) WriteDict(w, "inputSchema", InputSchema);
            }),
        };
        return frame;
    }

    public static PreenFrame FromQuackFrame(QuackFrame frame)
    {
        if (frame.Verb != QuackVerb.Preen)
            throw new InvalidOperationException($"Expected verb preen, got {frame.Verb}");
        var data = frame.Data ?? throw new InvalidOperationException("Preen frame requires data");
        return new PreenFrame
        {
            SkillId = data.GetProperty("skillId").GetString()!,
            Applicability = data.GetProperty("applicability").GetString()!,
            Constraints = ReadDict(data.GetProperty("constraints")),
            AssuranceLevel = data.GetProperty("assuranceLevel").GetString()!,
            ApplicabilityNotes = ReadOptStr(data, "applicabilityNotes"),
            PreExecutionGates = data.TryGetProperty("preExecutionGates", out var g) ? ReadStringArray(g) : [],
            EstimatedDuration = ReadOptStr(data, "estimatedDuration"),
            InputSchema = data.TryGetProperty("inputSchema", out var s) ? ReadDict(s) : null,
        }.ApplyBase<PreenFrame>(frame);
    }
}

public sealed record SettleFrame : NegotiateFrameBase
{
    public required string NegotiationId { get; init; }
    public required string SkillId { get; init; }
    public required Dictionary<string, object> AgreedConstraints { get; init; }
    public required string AgreedAssuranceLevel { get; init; }
    public required string ValidFrom { get; init; }
    public required string ValidUntil { get; init; }

    public override QuackFrame ToQuackFrame()
    {
        var frame = BuildBase(QuackVerb.Settle);
        frame = frame with
        {
            Data = WriteData(w =>
            {
                w.WriteString("negotiationId", NegotiationId);
                w.WriteString("skillId", SkillId);
                WriteDict(w, "agreedConstraints", AgreedConstraints);
                w.WriteString("agreedAssuranceLevel", AgreedAssuranceLevel);
                w.WriteString("validFrom", ValidFrom);
                w.WriteString("validUntil", ValidUntil);
            }),
        };
        return frame;
    }

    public static SettleFrame FromQuackFrame(QuackFrame frame)
    {
        if (frame.Verb != QuackVerb.Settle)
            throw new InvalidOperationException($"Expected verb settle, got {frame.Verb}");
        var data = frame.Data ?? throw new InvalidOperationException("Settle frame requires data");
        return new SettleFrame
        {
            NegotiationId = data.GetProperty("negotiationId").GetString()!,
            SkillId = data.GetProperty("skillId").GetString()!,
            AgreedConstraints = ReadDict(data.GetProperty("agreedConstraints")),
            AgreedAssuranceLevel = data.GetProperty("agreedAssuranceLevel").GetString()!,
            ValidFrom = data.GetProperty("validFrom").GetString()!,
            ValidUntil = data.GetProperty("validUntil").GetString()!,
        }.ApplyBase<SettleFrame>(frame);
    }
}

public sealed record ShunFrame : NegotiateFrameBase
{
    public required string ReasonCode { get; init; }
    public required string Reason { get; init; }
    public string[] SuggestedSkillIds { get; init; } = [];
    public Dictionary<string, object>? SuggestedConstraintRelaxation { get; init; }

    public override QuackFrame ToQuackFrame()
    {
        var frame = BuildBase(QuackVerb.Shun);
        frame = frame with
        {
            Data = WriteData(w =>
            {
                w.WriteString("reasonCode", ReasonCode);
                w.WriteString("reason", Reason);
                if (SuggestedSkillIds.Length > 0) WriteStringArray(w, "suggestedSkillIds", SuggestedSkillIds);
                if (SuggestedConstraintRelaxation is not null)
                    WriteDict(w, "suggestedConstraintRelaxation", SuggestedConstraintRelaxation);
            }),
        };
        return frame;
    }

    public static ShunFrame FromQuackFrame(QuackFrame frame)
    {
        if (frame.Verb != QuackVerb.Shun)
            throw new InvalidOperationException($"Expected verb shun, got {frame.Verb}");
        var data = frame.Data ?? throw new InvalidOperationException("Shun frame requires data");
        return new ShunFrame
        {
            ReasonCode = data.GetProperty("reasonCode").GetString()!,
            Reason = data.GetProperty("reason").GetString()!,
            SuggestedSkillIds = data.TryGetProperty("suggestedSkillIds", out var s) ? ReadStringArray(s) : [],
            SuggestedConstraintRelaxation = data.TryGetProperty("suggestedConstraintRelaxation", out var r)
                ? ReadDict(r) : null,
        }.ApplyBase<ShunFrame>(frame);
    }
}
