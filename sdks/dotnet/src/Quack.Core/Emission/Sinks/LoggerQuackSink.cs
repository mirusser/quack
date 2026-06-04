using Microsoft.Extensions.Logging;

namespace Quack;

/// <summary>
/// Writes frames to an <see cref="ILogger"/> as Quack-Text lines at Information level.
/// </summary>
public sealed class LoggerQuackSink : IQuackSink
{
    private readonly ILogger _logger;

    /// <param name="logger">The logger to write to.</param>
    public LoggerQuackSink(ILogger logger) => _logger = logger;

    /// <inheritdoc />
    public ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct = default)
    {
        _logger.LogInformation("{QuackFrame}", QuackText.Encode(frame));
        return new(QuackResult.Success(frame));
    }
}

/// <summary>
/// Typed variant of <see cref="LoggerQuackSink"/> using <see cref="ILogger{T}"/>.
/// </summary>
public sealed class LoggerQuackSink<T> : IQuackSink
{
    private readonly ILogger<T> _logger;

    /// <param name="logger">The typed logger.</param>
    public LoggerQuackSink(ILogger<T> logger) => _logger = logger;

    /// <inheritdoc />
    public ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct = default)
    {
        _logger.LogInformation("{QuackFrame}", QuackText.Encode(frame));
        return new(QuackResult.Success(frame));
    }
}
