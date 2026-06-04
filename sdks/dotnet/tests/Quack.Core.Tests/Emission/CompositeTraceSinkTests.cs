namespace Quack.Tests;

[TestClass]
public sealed class CompositeTraceSinkTests
{
    [TestMethod]
    public void Write_WhenInnerSinkThrows_ContinuesToRemainingSinks()
    {
        var counting = new CountingTraceSink();
        var sink = new CompositeTraceSink(new ITraceSink[] { new ThrowingTraceSink(), counting });
        var frame = QuackFrame.Quack("test");

        sink.Write(frame);

        Assert.AreEqual(1, counting.WriteCount);
    }

    [TestMethod]
    public void WriteRejection_WhenInnerSinkThrows_ContinuesToRemainingSinks()
    {
        var counting = new CountingTraceSink();
        var sink = new CompositeTraceSink(new ITraceSink[] { new ThrowingTraceSink(), counting });
        var frame = QuackFrame.Quack("test");
        var result = QuackResult.Rejected(new QuackError("TEST_ERROR", "test failure", QuackRule.InvalidFrame));

        sink.WriteRejection(frame, result);

        Assert.AreEqual(1, counting.WriteRejectionCount);
    }

    [TestMethod]
    public void Write_WithMultipleSinks_CallsAllSinks()
    {
        var first = new CountingTraceSink();
        var second = new CountingTraceSink();
        var sink = new CompositeTraceSink(new ITraceSink[] { first, second });
        var frame = QuackFrame.Quack("test");

        sink.Write(frame);

        Assert.AreEqual(1, first.WriteCount);
        Assert.AreEqual(1, second.WriteCount);
    }

    private sealed class CountingTraceSink : ITraceSink
    {
        public int WriteCount { get; private set; }

        public int WriteRejectionCount { get; private set; }

        public void Write(QuackFrame frame) => WriteCount++;

        public void WriteRejection(QuackFrame frame, QuackResult result) => WriteRejectionCount++;
    }

    private sealed class ThrowingTraceSink : ITraceSink
    {
        public void Write(QuackFrame frame) => throw new InvalidOperationException("test failure");

        public void WriteRejection(QuackFrame frame, QuackResult result) => throw new InvalidOperationException("test failure");
    }
}
