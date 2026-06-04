namespace Quack.Tests;

[TestClass]
public sealed class QuackFrameFactoryTests
{
    [TestMethod]
    public void QuackFactory_SetsMinimumFields()
    {
        var frame = QuackFrame.Quack("observer");

        Assert.AreEqual(1, frame.Version);
        Assert.AreEqual(QuackVerb.Quack, frame.Verb);
        Assert.AreEqual("observer", frame.Source);
        Assert.AreEqual(QuackRisk.None, frame.Risk);
        Assert.IsNotNull(frame.Id);
        Assert.AreEqual(26, frame.Id.Length);
        Assert.IsNotNull(frame.Timestamp);
    }

    [TestMethod]
    public void QuackFactory_WithAllOptions_SetsAllFields()
    {
        var frame = QuackFrame.Quack(
            source: "observer",
            destination: "listener",
            context: "k8s/default",
            correlation: "corr-1",
            risk: QuackRisk.Low,
            summary: "hello world",
            tone: QuackTone.PlayfulDuck);

        Assert.AreEqual("observer", frame.Source);
        Assert.AreEqual("listener", frame.Destination);
        Assert.AreEqual("k8s/default", frame.Context);
        Assert.AreEqual("corr-1", frame.Correlation);
        Assert.AreEqual(QuackRisk.Low, frame.Risk);
        Assert.AreEqual("hello world", frame.Summary);
        Assert.AreEqual(QuackTone.PlayfulDuck, frame.Tone);
    }

    [TestMethod]
    public void HonkFactory_SetsReasonInData()
    {
        var frame = QuackFrame.Honk("gateway", "digest mismatch");

        Assert.AreEqual(QuackVerb.Honk, frame.Verb);
        Assert.AreEqual("gateway", frame.Source);
        Assert.IsNotNull(frame.Data);
        Assert.IsTrue(frame.Data!.Value.TryGetProperty("reason", out var reason));
        Assert.AreEqual("digest mismatch", reason.GetString());
    }

    [TestMethod]
    public void SplashFactory_SetsEvidenceInData()
    {
        var evidence = new EvidenceRef[]
        {
            new() { Kind = "k8s.events", Digest = "sha256:abc", Uri = "artifact://events/default" },
        };

        var frame = QuackFrame.Splash("observer", evidence);

        Assert.AreEqual(QuackVerb.Splash, frame.Verb);
        Assert.IsNotNull(frame.Data);
        Assert.IsTrue(frame.Data!.Value.TryGetProperty("evidence", out var evidenceArray));
        Assert.AreEqual(JsonValueKind.Array, evidenceArray.ValueKind);
    }

    [TestMethod]
    public void MoltFactory_RequiresCorrelation()
    {
        var frame = QuackFrame.Molt("gateway", "plan-456");

        Assert.AreEqual(QuackVerb.Molt, frame.Verb);
        Assert.AreEqual("plan-456", frame.Correlation);
    }

    [TestMethod]
    public void PeckFactory_RequiresDestination()
    {
        var frame = QuackFrame.Peck("client", "server");

        Assert.AreEqual(QuackVerb.Peck, frame.Verb);
        Assert.AreEqual("client", frame.Source);
        Assert.AreEqual("server", frame.Destination);
    }

    [TestMethod]
    public void EggFactory_SetsDigest()
    {
        var frame = QuackFrame.Egg("planner", "executor", "plan-1", "sha256:abc123");

        Assert.AreEqual(QuackVerb.Egg, frame.Verb);
        Assert.AreEqual("sha256:abc123", frame.Digest);
        Assert.IsNotNull(frame.Data);
        Assert.IsTrue(frame.Data!.Value.TryGetProperty("eggId", out var eggId));
        Assert.AreEqual("plan-1", eggId.GetString());
    }

    [TestMethod]
    public void HatchFactory_RequiresEggIdAndCorrelation()
    {
        var frame = QuackFrame.Hatch("reviewer", "gateway", "plan-1", "corr-1");

        Assert.AreEqual(QuackVerb.Hatch, frame.Verb);
        Assert.AreEqual("corr-1", frame.Correlation);
        Assert.IsNotNull(frame.Data);
        Assert.IsTrue(frame.Data!.Value.TryGetProperty("eggId", out var eggId));
        Assert.AreEqual("plan-1", eggId.GetString());
    }

    [TestMethod]
    public void FlapFactory_RequiresEggIdDigestAndCorrelation()
    {
        var frame = QuackFrame.Flap("executor", "gateway", "plan-1", "sha256:abc", "corr-1");

        Assert.AreEqual(QuackVerb.Flap, frame.Verb);
        Assert.AreEqual("corr-1", frame.Correlation);
        Assert.AreEqual("sha256:abc", frame.Digest);
        Assert.IsNotNull(frame.Data);
        Assert.IsTrue(frame.Data!.Value.TryGetProperty("eggId", out _));
    }

    [TestMethod]
    public void BobFactory_SetsCorrelation()
    {
        var frame = QuackFrame.Bob("gateway", "planner", "corr-1");

        Assert.AreEqual(QuackVerb.Bob, frame.Verb);
        Assert.AreEqual("corr-1", frame.Correlation);
        Assert.AreEqual("planner", frame.Destination);
    }

    [TestMethod]
    public void NackFactory_SetsCorrelation()
    {
        var frame = QuackFrame.Nack("gateway", "planner", "corr-1");

        Assert.AreEqual(QuackVerb.Nack, frame.Verb);
        Assert.AreEqual("corr-1", frame.Correlation);
    }

    [TestMethod]
    public void PerchFactory_SetsCorrelation()
    {
        var frame = QuackFrame.Perch("executor", "planner", "corr-1");

        Assert.AreEqual(QuackVerb.Perch, frame.Verb);
        Assert.AreEqual("corr-1", frame.Correlation);
    }

    [TestMethod]
    public void AllFactories_GenerateUniqueIds()
    {
        var ids = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var frame = QuackFrame.Quack("test");
            Assert.IsTrue(ids.Add(frame.Id), $"Duplicate ULID: {frame.Id}");
        }
    }
}
