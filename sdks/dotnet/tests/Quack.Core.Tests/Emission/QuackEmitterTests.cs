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

[TestClass]
public sealed class QuackTextTests
{
    [TestMethod]
    public void Encode_MinimalFrame_ProducesExpectedFormat()
    {
        var frame = new QuackFrame
        {
            Version = 1,
            Verb = QuackVerb.Quack,
            Id = "01JQA1X5Z8W7M3N9P2R6V0K4B1",
            Source = "observer",
        };

        var text = QuackText.Encode(frame);

        Assert.IsTrue(text.StartsWith("QK1 quack"));
        Assert.IsTrue(text.Contains("quackId=01JQA1X5Z8W7M3N9P2R6V0K4B1"));
        Assert.IsTrue(text.Contains("src=observer"));
    }

    [TestMethod]
    public void Encode_WithAllFields_IncludesAllKeys()
    {
        var frame = QuackFrame.Quack(
            source: "test",
            destination: "target",
            context: "ctx",
            correlation: "corr",
            risk: QuackRisk.Medium,
            summary: "hello world",
            tone: QuackTone.PlayfulDuck);

        var text = QuackText.Encode(frame);

        Assert.IsTrue(text.Contains("destination=target"));
        Assert.IsTrue(text.Contains("context=ctx"));
        Assert.IsTrue(text.Contains("correlation=corr"));
        Assert.IsTrue(text.Contains("say=\"hello world\""));
    }

    [TestMethod]
    public void EncodeValue_WithoutSpecialChars_ReturnsUnchanged()
    {
        var result = QuackText.EncodeValue("simple");

        Assert.AreEqual("simple", result);
    }

    [TestMethod]
    public void EncodeValue_WithSpaces_IsQuoted()
    {
        var result = QuackText.EncodeValue("hello world");

        Assert.AreEqual("\"hello world\"", result);
    }

    [TestMethod]
    public void EncodeValue_WithEquals_IsQuoted()
    {
        var result = QuackText.EncodeValue("key=val");

        Assert.AreEqual("\"key=val\"", result);
    }
}
