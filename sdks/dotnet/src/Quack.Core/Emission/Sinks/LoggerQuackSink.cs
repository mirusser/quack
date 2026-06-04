using Microsoft.Extensions.Logging;

namespace Quack;

/// <summary>
/// Writes frames to an <see cref="ILogger"/> as Quack-Text lines at Information level.
/// </summary>
public sealed class LoggerQuackSink(ILogger logger) : IQuackSink
{
    private readonly ILogger _logger = logger;

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
public sealed class LoggerQuackSink<T>(ILogger<T> logger) : IQuackSink
{
    private readonly ILogger<T> _logger = logger;

    /// <inheritdoc />
    public ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct = default)
    {
        _logger.LogInformation("{QuackFrame}", QuackText.Encode(frame));
        return new(QuackResult.Success(frame));
    }
}
