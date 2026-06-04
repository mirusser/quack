using System.Text.Json;

namespace Quack.Negotiate;

public sealed class NegotiateValidator
{
    public NegotiateValidationResult Validate(NegotiateFrameBase frame, IQuackHistory history,
        QuackRisk maxRisk = QuackRisk.Critical)
    {
        var errors = new List<NegotiateError>();

        // Spec §3: destination is always required for negotiation frames
        if (string.IsNullOrEmpty(frame.Destination))
            errors.Add(new NegotiateError("NEGOTIATION_REQUIRES_DESTINATION",
                "Negotiation frames are always point-to-point and require a destination"));

        // Spec §6.10: reject frames exceeding maxRisk
        if (frame.Risk > maxRisk)
            errors.Add(new NegotiateError("RISK_ABOVE_MAX",
                $"Frame risk '{frame.Risk}' exceeds maximum accepted risk '{maxRisk}'"));

        // Spec §6.12: reject expired frames (expiresAt in the past)
        if (frame.ExpiresAt is { } expiresAt &&
            DateTimeOffset.TryParse(expiresAt, out var expiresAtParsed) &&
            expiresAtParsed < DateTimeOffset.UtcNow)
        {
            errors.Add(new NegotiateError("FRAME_EXPIRED",
                $"Frame expired at {expiresAt}"));
        }

        switch (frame)
        {
            case PreenFrame preen:
                ValidatePreen(preen, history, errors);
                break;
            case SettleFrame settle:
                ValidateSettle(settle, history, errors);
                break;
        }

        return errors.Count == 0
            ? NegotiateValidationResult.Valid()
            : NegotiateValidationResult.Invalid(errors);
    }

    private static void ValidatePreen(PreenFrame preen, IQuackHistory history, List<NegotiateError> errors)
    {
        if (preen.Correlation is not { } corr)
        {
            errors.Add(new NegotiateError("PREEN_REQUIRES_DABBLE", "Preen requires a correlation referencing a prior dabble"));
            return;
        }

        var frames = history.GetByCorrelation(corr);
        if (!frames.Any(f => f.Verb == QuackVerb.Dabble))
            errors.Add(new NegotiateError("PREEN_REQUIRES_DABBLE", "Preen requires a prior dabble with the same correlation"));
    }

    private static void ValidateSettle(SettleFrame settle, IQuackHistory history, List<NegotiateError> errors)
    {
        if (settle.Correlation is not { } corr)
        {
            errors.Add(new NegotiateError("SETTLE_REQUIRES_PREEN", "Settle requires a correlation referencing a prior preen"));
            return;
        }

        var frames = history.GetByCorrelation(corr);
        var preenFrame = frames.FirstOrDefault(f => f.Verb == QuackVerb.Preen);

        if (preenFrame is null || preenFrame.Data is not { } preenData)
        {
            errors.Add(new NegotiateError("SETTLE_REQUIRES_PREEN", "Settle requires a prior preen with the same correlation"));
            return;
        }

        // Constraint relaxation check — settle must not relax preen constraints
        if (preenData.TryGetProperty("constraints", out var preenConstraints) &&
            preenConstraints.ValueKind == JsonValueKind.Object)
        {
            foreach (var agreedKvp in settle.AgreedConstraints)
            {
                if (!preenConstraints.TryGetProperty(agreedKvp.Key, out _))
                    errors.Add(new NegotiateError("SETTLE_CONSTRAINT_MISMATCH",
                        $"Settle adds constraint '{agreedKvp.Key}' not present in preen"));
            }
        }

        // Spec §6.11: reject settle where validUntil is in the past or validFrom > validUntil
        if (DateTimeOffset.TryParse(settle.ValidUntil, out var until) && until < DateTimeOffset.UtcNow)
            errors.Add(new NegotiateError("SETTLE_OUTSIDE_VALIDITY",
                $"Settle validity window expired at {settle.ValidUntil}"));

        if (DateTimeOffset.TryParse(settle.ValidFrom, out var from) &&
            DateTimeOffset.TryParse(settle.ValidUntil, out var to) && from > to)
            errors.Add(new NegotiateError("SETTLE_INVALID_WINDOW",
                $"ValidFrom ({settle.ValidFrom}) is after ValidUntil ({settle.ValidUntil})"));
    }

}

public sealed record NegotiateError(string Code, string Message);

public sealed class NegotiateValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<NegotiateError> Errors { get; }

    private NegotiateValidationResult(bool isValid, IReadOnlyList<NegotiateError> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public static NegotiateValidationResult Valid() => new(true, Array.Empty<NegotiateError>());
    public static NegotiateValidationResult Invalid(IReadOnlyList<NegotiateError> errors) => new(false, errors);
}
