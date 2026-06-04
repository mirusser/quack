namespace Quack.Tests;

[TestClass]
public sealed class QuackEmitterTests
{
    [TestMethod]
    public async Task Emitter_AutoSetsIdTimestampSource()
    {
        var options = new QuackOptions { AgentName = "test-agent" };
        var emitter = new QuackEmitter(new NoopQuackSink(), new QuackValidator(), options);
        var frame = new QuackFrame { Verb = QuackVerb.Quack };

        var result = await emitter.EmitAsync(frame);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Frame);
        Assert.IsFalse(string.IsNullOrEmpty(result.Frame!.Id));
        Assert.AreEqual("test-agent", result.Frame!.Source);
        Assert.AreEqual(1, result.Frame!.Version);
        Assert.IsNotNull(result.Frame!.Timestamp);
    }

    [TestMethod]
    public async Task Emitter_KeepsExistingIdSource()
    {
        var options = new QuackOptions { AgentName = "test-agent" };
        var emitter = new QuackEmitter(new NoopQuackSink(), new QuackValidator(), options);
        var frame = QuackFrame.Quack("custom-source");

        var result = await emitter.EmitAsync(frame);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("custom-source", result.Frame!.Source);
    }

    [TestMethod]
    public async Task Emitter_RiskGate_RejectsHighRiskWhenMaxIsLow()
    {
        var options = new QuackOptions { AgentName = "test", MaxRisk = QuackRisk.Low };
        var emitter = new QuackEmitter(new NoopQuackSink(), new QuackValidator(), options);
        var frame = QuackFrame.Quack("test", risk: QuackRisk.High);

        var result = await emitter.EmitAsync(frame);

        Assert.IsTrue(result.IsRejected);
        Assert.IsTrue(result.Errors.Any(e => e.Rule == QuackRule.RiskExceedsMax));
    }

    [TestMethod]
    public async Task Emitter_TtlGate_RejectsExcessiveTtl()
    {
        var options = new QuackOptions { AgentName = "test", MaxTtl = 1000 };
        var emitter = new QuackEmitter(new NoopQuackSink(), new QuackValidator(), options);
        var frame = QuackFrame.Quack("test") with { Ttl = 5000 };

        var result = await emitter.EmitAsync(frame);

        Assert.IsTrue(result.IsRejected);
        Assert.IsTrue(result.Errors.Any(e => e.Rule == QuackRule.TtlExceedsMax));
    }

    [TestMethod]
    public async Task Emitter_ValidationRejectsInvalidFrame()
    {
        var options = new QuackOptions { AgentName = "test" };
        var emitter = new QuackEmitter(new NoopQuackSink(), new QuackValidator(), options);
        var frame = new QuackFrame
        {
            Version = 1,
            Verb = QuackVerb.Peck,
            Id = QuackId.NewId(),
            Source = "client",
            // missing destination — should fail validation
        };

        var result = await emitter.EmitAsync(frame);

        Assert.IsTrue(result.IsRejected);
        Assert.AreEqual(QuackRule.NonBroadcastWithoutDestination, result.Errors[0].Rule);
    }

    [TestMethod]
    public async Task Emitter_VersionExceedsMaxQuackVersion_ReturnsInvalidFrame()
    {
        var options = new QuackOptions { AgentName = "test", MaxQuackVersion = 1 };
        var emitter = new QuackEmitter(new NoopQuackSink(), new QuackValidator(), options);
        var frame = QuackFrame.Quack("test") with { Version = 2 };

        var result = await emitter.EmitAsync(frame);

        Assert.IsTrue(result.IsRejected);
        Assert.AreEqual("VERSION_EXCEEDS_MAX", result.Errors[0].Code);
        Assert.AreEqual(QuackRule.InvalidFrame, result.Errors[0].Rule);
    }

    [TestMethod]
    public async Task Emitter_ValidFrame_Delivers()
    {
        var options = new QuackOptions { AgentName = "test" };
        var counting = new CountingSink();
        var emitter = new QuackEmitter(counting, new QuackValidator(), options);
        var frame = new QuackFrame
        {
            Verb = QuackVerb.Quack,
            Id = QuackId.NewId(),
            Source = "test",
        };

        var result = await emitter.EmitAsync(frame);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, counting.EmitCount);
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
}
