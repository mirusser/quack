using Microsoft.Extensions.Logging;

namespace Quack.Tests;

[TestClass]
public sealed class LoggerQuackSinkTests
{
    [TestMethod]
    public async Task EmitAsync_WithFrame_LogsQuackTextAndReturnsSuccess()
    {
        using var factory = LoggerFactory.Create(b => b.AddConsole());
        var logger = factory.CreateLogger("test");
        var sink = new LoggerQuackSink(logger);
        var frame = QuackFrame.Quack("test");

        var result = await sink.EmitAsync(frame);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreSame(frame, result.Frame);
    }

    [TestMethod]
    public async Task EmitAsync_TypedVariant_ReturnsSuccess()
    {
        using var factory = LoggerFactory.Create(b => b.AddConsole());
        var logger = factory.CreateLogger<LoggerQuackSinkTests>();
        var sink = new LoggerQuackSink<LoggerQuackSinkTests>(logger);
        var frame = QuackFrame.Quack("test");

        var result = await sink.EmitAsync(frame);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreSame(frame, result.Frame);
    }
}
