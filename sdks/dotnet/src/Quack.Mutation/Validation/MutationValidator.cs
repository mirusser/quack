using System.Text.Json;

namespace Quack.Mutation;

public sealed class MutationValidator
{
    public MutationValidationResult Validate(MutationFrameBase frame, IQuackHistory history,
        QuackRisk maxRisk = QuackRisk.Critical)
    {
        var errors = new List<MutationError>();

        // Spec §6.11: reject frames exceeding maxRisk
        if (frame.Risk > maxRisk)
            errors.Add(new MutationError("RISK_ABOVE_MAX",
                $"Frame risk '{frame.Risk}' exceeds maximum accepted risk '{maxRisk}'"));

        // Spec §6.12: reject frames whose expiresAt is in the past
        if (frame.ExpiresAt is { } expiresAt &&
            DateTimeOffset.TryParse(expiresAt, out var expiresAtParsed) &&
            expiresAtParsed < DateTimeOffset.UtcNow)
        {
            errors.Add(new MutationError("FRAME_EXPIRED", $"Frame expired at {expiresAt}"));
        }

        switch (frame)
        {
            case FlapFrame flap:
                ValidateFlap(flap, history, errors);
                break;
            case EggFrame egg:
                ValidateEgg(egg, history, errors);
                break;
            case HatchFrame hatch:
                ValidateHatch(hatch, history, errors);
                break;
        }

        return errors.Count == 0
            ? MutationValidationResult.Valid()
            : MutationValidationResult.Invalid(errors);
    }

    private static void ValidateFlap(FlapFrame flap, IQuackHistory history, List<MutationError> errors)
    {
        if (flap.Correlation is not { } corr)
        {
            errors.Add(new MutationError("FLAP_REQUIRES_CORRELATION", "Flap requires a correlation"));
            return;
        }

        var frames = history.GetByCorrelation(corr);
        var eggFrame = frames.FirstOrDefault(f => f.Verb == QuackVerb.Egg);
        var hatchFrame = frames.FirstOrDefault(f => f.Verb == QuackVerb.Hatch);
        var bobFrame = frames.FirstOrDefault(f => f.Verb == QuackVerb.Bob);

        if (eggFrame is null || eggFrame.Data is not { } eggData)
        {
            errors.Add(new MutationError("FLAP_REQUIRES_EGG", "Flap requires a prior egg with the same correlation"));
            return;
        }

        // Check digests match the egg
        var eggIntentDigest = GetStringProp(eggData, "intentDigest");
        var eggReviewDigest = GetStringProp(eggData, "reviewDigest");

        if (!string.Equals(flap.IntentDigest, eggIntentDigest, StringComparison.Ordinal))
            errors.Add(new MutationError("DIGEST_MISMATCH", $"Intent digest mismatch: flap has {flap.IntentDigest}, egg has {eggIntentDigest}"));

        if (!string.Equals(flap.ReviewDigest, eggReviewDigest, StringComparison.Ordinal))
            errors.Add(new MutationError("DIGEST_MISMATCH", $"Review digest mismatch: flap has {flap.ReviewDigest}, egg has {eggReviewDigest}"));

        // Check plan validity window
        var validFrom = GetStringProp(eggData, "validFrom");
        var validUntil = GetStringProp(eggData, "validUntil");

        if (validFrom is not null && validUntil is not null)
        {
            if (DateTimeOffset.TryParse(validUntil, out var until) && until < DateTimeOffset.UtcNow)
                errors.Add(new MutationError("PLAN_EXPIRED", $"Plan validity expired at {validUntil}"));
        }

        // Check for approval grant (bob) — must have hatch + bob in same correlation
        if (bobFrame is null)
            errors.Add(new MutationError("FLAP_REQUIRES_GRANT", "Flap requires a prior bob (approval grant) with the same correlation"));

        // Spec §6.9: single-execution — reject if a successful perch already exists for this planId
        var planId = GetStringProp(eggData, "planId");
        if (planId is not null)
        {
            var hasSuccessfulExecution = frames
                .Where(f => f.Verb == QuackVerb.Perch && f.Data.HasValue)
                .Any(f =>
                {
                    var data = f.Data!.Value;
                    return GetStringProp(data, "planId") == planId &&
                           GetStringProp(data, "outcome") == "succeeded";
                });

            if (hasSuccessfulExecution)
                errors.Add(new MutationError("SINGLE_EXECUTION_VIOLATION",
                    $"Plan '{planId}' has already been executed successfully. single-execution policy prevents reuse."));
        }
    }

    private static void ValidateEgg(EggFrame egg, IQuackHistory history, List<MutationError> errors)
    {
        // egg requires prior splash in same context
        if (egg.Context is not { } ctx)
            return;

        var ctxFrames = history.GetByContext(ctx);
        if (!ctxFrames.Any(f => f.Verb == QuackVerb.Splash))
            errors.Add(new MutationError("EGG_REQUIRES_SPLASH", "Egg requires a prior splash in the same context"));
    }

    private static void ValidateHatch(HatchFrame hatch, IQuackHistory history, List<MutationError> errors)
    {
        if (hatch.Correlation is not { } corr)
        {
            errors.Add(new MutationError("HATCH_REQUIRES_CORRELATION", "Hatch requires a correlation"));
            return;
        }

        var frames = history.GetByCorrelation(corr);
        var eggFrame = frames.FirstOrDefault(f => f.Verb == QuackVerb.Egg);

        if (eggFrame is null || eggFrame.Data is not { } eggData)
        {
            errors.Add(new MutationError("HATCH_REQUIRES_EGG", "Hatch requires a prior egg with the same correlation"));
            return;
        }

        // Check digests match
        var eggIntentDigest = GetStringProp(eggData, "intentDigest");
        var eggReviewDigest = GetStringProp(eggData, "reviewDigest");

        if (!string.Equals(hatch.IntentDigest, eggIntentDigest, StringComparison.Ordinal))
            errors.Add(new MutationError("DIGEST_MISMATCH", $"Intent digest mismatch between hatch and egg"));

        if (!string.Equals(hatch.ReviewDigest, eggReviewDigest, StringComparison.Ordinal))
            errors.Add(new MutationError("DIGEST_MISMATCH", $"Review digest mismatch between hatch and egg"));
    }

    private static string? GetStringProp(JsonElement data, string name) =>
        data.TryGetProperty(name, out var val) && val.ValueKind == JsonValueKind.String
            ? val.GetString() : null;
}

public sealed record MutationError(string Code, string Message);

public sealed class MutationValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<MutationError> Errors { get; }

    private MutationValidationResult(bool isValid, IReadOnlyList<MutationError> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public static MutationValidationResult Valid() => new(true, Array.Empty<MutationError>());
    public static MutationValidationResult Invalid(IReadOnlyList<MutationError> errors) => new(false, errors);
}
