namespace Quack;

/// <summary>
/// Default implementation of <see cref="IQuackEmitter"/>.
/// Auto-sets Id, Timestamp, Source, and Version on outgoing frames,
/// gates on Risk and TTL, then fans out through the sink pipeline.
/// </summary>
public sealed class QuackEmitter : IQuackEmitter
{
    private readonly IQuackSink _sink;
    private readonly IQuackValidator _validator;
    private readonly ITraceSink? _traceSink;
    private readonly QuackOptions _options;

    /// <param name="sink">The delivery sink pipeline.</param>
    /// <param name="validator">The frame validator.</param>
    /// <param name="traceSink">Optional trace sink for rejection observability.</param>
    /// <param name="options">Configuration options.</param>
    public QuackEmitter(
        IQuackSink sink,
        IQuackValidator validator,
        QuackOptions options,
        ITraceSink? traceSink = null)
    {
        _sink = sink;
        _validator = validator;
        _options = options;
        _traceSink = traceSink;
    }

    /// <inheritdoc />
    public string AgentName => _options.AgentName;

    /// <inheritdoc />
    public async ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct = default)
    {
        // Auto-set properties
        frame = frame with
        {
            Id = string.IsNullOrEmpty(frame.Id) ? QuackId.NewId() : frame.Id,
            Timestamp = frame.Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Source = string.IsNullOrEmpty(frame.Source) ? _options.AgentName : frame.Source,
            Version = frame.Version < 1 ? 1 : frame.Version,
        };

        // Version gate
        if (frame.Version > _options.MaxQuackVersion)
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
}
