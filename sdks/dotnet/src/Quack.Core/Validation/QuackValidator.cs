namespace Quack;

/// <summary>
/// Default validator implementation. Enforces all protocol rules from §3 of the spec.
/// </summary>
public sealed class QuackValidator : IQuackValidator
{
    private static readonly HashSet<QuackVerb> BroadcastVerbs = new()
    {
        QuackVerb.Quack, QuackVerb.Honk, QuackVerb.Splash, QuackVerb.Molt,
    };

    /// <inheritdoc />
    public ValidationResult Validate(QuackFrame frame)
    {
        var errors = new List<QuackError>();

        // Basic presence checks
        if (frame.Version < 1)
            errors.Add(Err(QuackRule.InvalidFrame, "Version must be >= 1"));

        if (string.IsNullOrEmpty(frame.Id))
            errors.Add(Err(QuackRule.InvalidFrame, "Id is required"));
        else if (!IsValidUlid(frame.Id))
            errors.Add(Err(QuackRule.InvalidFrame, "Id must be a valid ULID"));

        if (string.IsNullOrEmpty(frame.Source))
            errors.Add(Err(QuackRule.InvalidFrame, "Source is required"));

        ValidateStructural(frame, errors);

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors.ToArray());
    }

    /// <inheritdoc />
    public ValidationResult Validate(QuackFrame frame, IQuackHistory history)
    {
        var result = Validate(frame);
        if (!result.IsValid)
            return result;

        var errors = new List<QuackError>();
        ValidateSequencing(frame, history, errors);

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors.ToArray());
    }

    private static void ValidateStructural(QuackFrame frame, List<QuackError> errors)
    {
        // §3.2.5 — honk must have data.reason
        if (frame.Verb == QuackVerb.Honk)
        {
            if (frame.Data is not { } data || !data.TryGetProperty("reason", out _))
                errors.Add(Err(QuackRule.HonkWithoutReason, "Honk requires data.reason"));
        }

        // §3.2.6 — molt must carry correlation
        if (frame.Verb == QuackVerb.Molt)
        {
            if (string.IsNullOrEmpty(frame.Correlation))
                errors.Add(Err(QuackRule.MoltWithoutCorrelation, "Molt requires a non-null correlation"));
        }

        // §3.2.7 — splash must have data.evidence (array, ≥1)
        if (frame.Verb == QuackVerb.Splash)
        {
            if (frame.Data is not { } splashData ||
                !splashData.TryGetProperty("evidence", out var evidence) ||
                evidence.ValueKind != JsonValueKind.Array ||
                evidence.GetArrayLength() == 0)
            {
                errors.Add(Err(QuackRule.SplashWithoutEvidence,
                    "Splash requires data.evidence (non-empty array)"));
            }
        }

        // §3.2.8 — non-broadcast verb must carry destination
        if (!BroadcastVerbs.Contains(frame.Verb))
        {
            if (string.IsNullOrEmpty(frame.Destination))
                errors.Add(Err(QuackRule.NonBroadcastWithoutDestination,
                    "Non-broadcast verb requires a destination"));
        }
    }

    private static void ValidateSequencing(QuackFrame frame, IQuackHistory history, List<QuackError> errors)
    {
        // §3.1.4 — egg must carry digest
        if (frame.Verb == QuackVerb.Egg)
        {
            if (string.IsNullOrEmpty(frame.Digest))
                errors.Add(Err(QuackRule.EggWithoutProof, "Egg requires a digest"));
        }

        // §3.1.3 — egg requires prior splash in same context (uses context, not correlation)
        if (frame.Verb == QuackVerb.Egg && frame.Context is { } ctx)
        {
            var byContext = history.GetByContext(ctx);
            if (!byContext.Any(f => f.Verb == QuackVerb.Splash))
                errors.Add(Err(QuackRule.EggWithoutSplash, "Egg requires a prior splash in the same context"));
        }

        if (frame.Correlation is not { } corr)
            return;

        var byCorrelation = history.GetByCorrelation(corr);

        // §3.1.2 — hatch requires prior egg in same correlation
        if (frame.Verb == QuackVerb.Hatch)
        {
            if (!byCorrelation.Any(f => f.Verb == QuackVerb.Egg))
                errors.Add(Err(QuackRule.HatchWithoutEgg, "Hatch requires a prior egg with the same correlation"));
        }

        // §3.1.1 — flap requires prior hatch + bob in same correlation
        if (frame.Verb == QuackVerb.Flap)
        {
            var hasHatch = byCorrelation.Any(f => f.Verb == QuackVerb.Hatch);
            var hasBob = byCorrelation.Any(f => f.Verb == QuackVerb.Bob);
            if (!hasHatch || !hasBob)
                errors.Add(Err(QuackRule.FlapWithoutHatch, "Flap requires prior hatch + bob with the same correlation"));
        }
    }

    private static QuackError Err(QuackRule rule, string message) =>
        new(FormatCode(rule), message, rule);

    private static bool IsValidUlid(string id)
    {
        // ULID: 26 chars, Crockford Base32 alphabet
        if (id.Length != 26)
            return false;

        // Crockford's Base32: 0-9, A-Z excluding I, L, O, U
        const string alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        foreach (var c in id)
        {
            if (!alphabet.Contains(char.ToUpperInvariant(c)))
                return false;
        }

        return true;
    }

    private static string FormatCode(QuackRule rule) => rule switch
    {
        QuackRule.FlapWithoutHatch => "FLAP_WITHOUT_HATCH",
        QuackRule.HatchWithoutEgg => "HATCH_WITHOUT_EGG",
        QuackRule.EggWithoutSplash => "EGG_WITHOUT_SPLASH",
        QuackRule.EggWithoutProof => "EGG_WITHOUT_PROOF",
        QuackRule.HonkWithoutReason => "HONK_WITHOUT_REASON",
        QuackRule.MoltWithoutCorrelation => "MOLT_WITHOUT_CORRELATION",
        QuackRule.SplashWithoutEvidence => "SPLASH_WITHOUT_EVIDENCE",
        QuackRule.NonBroadcastWithoutDestination => "NON_BROADCAST_WITHOUT_DESTINATION",
        QuackRule.RiskExceedsMax => "RISK_EXCEEDS_MAX",
        QuackRule.TtlExceedsMax => "TTL_EXCEEDS_MAX",
        QuackRule.InvalidFrame => "INVALID_FRAME",
        _ => rule.ToString().ToUpperInvariant(),
    };
}
