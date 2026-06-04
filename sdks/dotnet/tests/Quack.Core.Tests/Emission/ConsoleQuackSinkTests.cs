namespace Quack.Tests;

[TestClass]
public sealed class ConsoleQuackSinkTests
{
    [TestMethod]
    public async Task EmitAsync_LowRisk_WritesStdout()
    {
        var originalOut = Console.Out;
        using var writer = new StringWriter();

        try
        {
            Console.SetOut(writer);
            var sink = new ConsoleQuackSink();
            var frame = QuackFrame.Quack("test");

            await sink.EmitAsync(frame);

            Assert.Contains("QK1", writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [TestMethod]
    public async Task EmitAsync_HighRisk_WritesStderr()
    {
        var originalError = Console.Error;
        using var writer = new StringWriter();

        try
        {
            Console.SetError(writer);
            var sink = new ConsoleQuackSink();
            var frame = QuackFrame.Quack("test") with { Risk = QuackRisk.High };

            await sink.EmitAsync(frame);

            Assert.Contains("QK1", writer.ToString());
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [TestMethod]
    public async Task EmitAsync_ReturnsSuccess()
    {
        var originalOut = Console.Out;
        using var writer = new StringWriter();

        try
        {
            Console.SetOut(writer);
            var sink = new ConsoleQuackSink();
            var frame = QuackFrame.Quack("test");

            var result = await sink.EmitAsync(frame);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreSame(frame, result.Frame);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
