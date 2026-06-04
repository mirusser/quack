namespace Quack;

/// <summary>
/// Default implementation of <see cref="IQuackEmitter"/>.
/// Auto-sets Id, Timestamp, Source, and Version on outgoing frames,
/// gates on Risk and TTL, then fans out through the sink pipeline.
/// </summary>
public sealed class QuackEmitter(
    IQuackSink sink,
    IQuackValidator validator,
    QuackOptions options,
    ITraceSink? traceSink = null) : IQuackEmitter
{
    private readonly IQuackSink _sink = sink;
    private readonly IQuackValidator _validator = validator;
    private readonly ITraceSink? _traceSink = traceSink;
    private readonly QuackOptions _options = options;

    /// <inheritdoc />
    public string AgentName => _options.AgentName;

    /// <inheritdoc />
    public async ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct = default)
    {
        // Auto-set properties
        frame = frame with
        {
            Id = string.IsNullOrEmpty(frame.Id) ? QuackId.NewId() : frame.Id,
            Timestamp = frame.Timestamp ?? DateTimeOffset.UtcNow.ToString("o"),
            Source = string.IsNullOrEmpty(frame.Source) ? _options.AgentName : frame.Source,
            Version = string.IsNullOrEmpty(frame.Version) ? "0.1" : frame.Version,
        };

        // Version gate
        if (CompareVersions(frame.Version, _options.MaxQuackVersion) > 0)
        {
            var err = new QuackError("VERSION_EXCEEDS_MAX", $"Version {frame.Version} exceeds max {_options.MaxQuackVersion}", QuackRule.InvalidFrame);
            var result = QuackResult.Rejected(err);
            _traceSink?.WriteRejection(frame, result);
            return result;
        }

        // Risk gate
        if (frame.Risk > _options.MaxRisk)
        {
            var err = new QuackError("RISK_EXCEEDS_MAX", $"Risk {frame.Risk} exceeds max {_options.MaxRisk}", QuackRule.RiskExceedsMax);
            var result = QuackResult.Rejected(err);
            _traceSink?.WriteRejection(frame, result);
            return result;
        }

        // TTL gate
        if (_options.MaxTtl is { } maxTtl && frame.Ttl is { } frameTtl && frameTtl > maxTtl)
        {
            var err = new QuackError("TTL_EXCEEDS_MAX", $"TTL {frameTtl} exceeds max {maxTtl}", QuackRule.TtlExceedsMax);
            var result = QuackResult.Rejected(err);
            _traceSink?.WriteRejection(frame, result);
            return result;
        }

        // Validate
        var validation = _validator.Validate(frame);
        if (!validation.IsValid)
        {
            var result = QuackResult.Rejected(validation.Errors.ToArray());
            _traceSink?.WriteRejection(frame, result);
            return result;
        }

        // Deliver
        var deliveryResult = await _sink.EmitAsync(frame, ct).ConfigureAwait(false);
        if (deliveryResult.IsRejected)
            _traceSink?.WriteRejection(frame, deliveryResult);
        return deliveryResult;
    }

    private static int CompareVersions(string a, string b)
    {
        if (Version.TryParse(a, out var va) && Version.TryParse(b, out var vb))
            return va.CompareTo(vb);
        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
