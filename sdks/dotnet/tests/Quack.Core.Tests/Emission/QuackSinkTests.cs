namespace Quack.Tests;

[TestClass]
public sealed class QuackSinkTests
{
    [TestMethod]
    public async Task NoopQuackSink_ReturnsSuccess()
    {
        var sink = new NoopQuackSink();
        var frame = QuackFrame.Quack("test");

        var result = await sink.EmitAsync(frame);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Frame);
        Assert.AreEqual("test", result.Frame!.Source);
    }

    [TestMethod]
    public async Task CompositeQuackSink_FansOutToAllSinks()
    {
        var sink1 = new CountingSink();
        var sink2 = new CountingSink();
        var composite = new CompositeQuackSink(sink1, sink2);
        var frame = QuackFrame.Quack("test");

        await composite.EmitAsync(frame);

        Assert.AreEqual(1, sink1.EmitCount);
        Assert.AreEqual(1, sink2.EmitCount);
    }

    [TestMethod]
    public async Task CompositeQuackSink_ContinueOnError_ReturnsFirstError()
    {
        var failing = new FailingSink();
        var succeeding = new NoopQuackSink();
        var composite = new CompositeQuackSink(failing, succeeding);
        var frame = QuackFrame.Quack("test");

        var result = await composite.EmitAsync(frame);

        Assert.IsTrue(result.IsRejected);
    }

    [TestMethod]
    public async Task CompositeQuackSink_ContinueMode_WhenFirstSinkFails_CallsRemainingSinksAndReturnsFirstError()
    {
        var failing = new FailingSink();
        var counting = new CountingSink();
        var composite = new CompositeQuackSink(
            new IQuackSink[] { failing, counting },
            CompositeFailureMode.Continue);
        var frame = QuackFrame.Quack("test");

        var result = await composite.EmitAsync(frame);

        Assert.AreEqual(1, counting.EmitCount);
        Assert.IsTrue(result.IsRejected);
        Assert.AreEqual("TEST_ERROR", result.Errors[0].Code);
    }

    [TestMethod]
    public async Task CompositeQuackSink_WithNoSinks_ReturnsSuccess()
    {
        var composite = new CompositeQuackSink(Array.Empty<IQuackSink>());
        var frame = QuackFrame.Quack("test");

        var result = await composite.EmitAsync(frame);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(frame, result.Frame);
    }

    [TestMethod]
    public async Task CompositeQuackSink_StopOnError_StopsImmediately()
    {
        var failing = new FailingSink();
        var sink2 = new CountingSink();
        var composite = new CompositeQuackSink(
            new IQuackSink[] { failing, sink2 },
            CompositeFailureMode.Stop);
        var frame = QuackFrame.Quack("test");

        await composite.EmitAsync(frame);

        Assert.AreEqual(0, sink2.EmitCount, "Second sink should not be called in Stop mode");
    }

    private sealed class CountingSink : IQuackSink
    {
        public int EmitCount { get; private set; }
        public ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct)
        {
            EmitCount++;
            return new(QuackResult.Success(frame));
        }
    }

    private sealed class FailingSink : IQuackSink
    {
        public ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct) =>
            new(QuackResult.Rejected(new QuackError("TEST_ERROR", "test failure", QuackRule.InvalidFrame)));
    }
}
