namespace Quack.Tests;

[TestClass]
public sealed class CompositeQuackSinkTests
{
    [TestMethod]
    public async Task EmitAsync_ContinueMode_WhenFirstSinkFails_CallsRemainingSinksAndReturnsFirstError()
    {
        var failing = new FailingSink();
        var counting = new CountingSink();
        var sink = new CompositeQuackSink(new IQuackSink[] { failing, counting });
        var frame = QuackFrame.Quack("test");

        var result = await sink.EmitAsync(frame);

        Assert.AreEqual(1, failing.EmitCount);
        Assert.AreEqual(1, counting.EmitCount);
        Assert.IsTrue(result.IsRejected);
        Assert.AreEqual(FailingSink.ErrorCode, result.Errors[0].Code);
    }

    [TestMethod]
    public async Task EmitAsync_WithNoSinks_ReturnsSuccess()
    {
        var sink = new CompositeQuackSink();
        var frame = QuackFrame.Quack("test");

        var result = await sink.EmitAsync(frame);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreSame(frame, result.Frame);
    }

    [TestMethod]
    public async Task EmitAsync_StopMode_StopsOnFirstError()
    {
        var failing = new FailingSink();
        var counting = new CountingSink();
        var sink = new CompositeQuackSink(
            new IQuackSink[] { failing, counting },
            CompositeFailureMode.Stop);
        var frame = QuackFrame.Quack("test");

        var result = await sink.EmitAsync(frame);

        Assert.AreEqual(1, failing.EmitCount);
        Assert.AreEqual(0, counting.EmitCount);
        Assert.IsTrue(result.IsRejected);
    }

    [TestMethod]
    public async Task EmitAsync_FansOutToAllSinks()
    {
        var first = new CountingSink();
        var second = new CountingSink();
        var sink = new CompositeQuackSink(first, second);
        var frame = QuackFrame.Quack("test");

        var result = await sink.EmitAsync(frame);

        Assert.AreEqual(1, first.EmitCount);
        Assert.AreEqual(1, second.EmitCount);
        Assert.IsTrue(result.IsSuccess);
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
        public const string ErrorCode = "TEST_ERROR";

        public int EmitCount { get; private set; }

        public ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct)
        {
            EmitCount++;
            return new(QuackResult.Rejected(new QuackError(ErrorCode, "test failure", QuackRule.InvalidFrame)));
        }
    }
}
