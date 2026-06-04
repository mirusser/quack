namespace Quack.Json.Tests;

[TestClass]
public sealed class QuackJsonTests
{
    [TestMethod]
    public void Serialize_MinimalFrame_ProducesJson()
    {
        var frame = QuackFrame.Quack("observer");
        var json = QuackJson.Serialize(frame);

        Assert.IsNotNull(json);
        Assert.IsTrue(json.Contains("\"verb\":\"quack\""));
        Assert.IsTrue(json.Contains("\"source\":\"observer\""));
        Assert.IsTrue(json.Contains("\"id\":"));
        Assert.IsTrue(json.Contains("\"version\":1"));
    }

    [TestMethod]
    public void Serialize_Risk_SerializesAsSingleChar()
    {
        var frame = QuackFrame.Quack("test", risk: QuackRisk.High);
        var json = QuackJson.Serialize(frame);

        Assert.IsTrue(json.Contains("\"risk\":\"h\""));
    }

    [TestMethod]
    public void Deserialize_Risk_CaseInsensitive()
    {
        var json = "{\"version\":1,\"verb\":\"quack\",\"id\":\"01JQA1\",\"source\":\"test\",\"risk\":\"H\"}";
        var frame = QuackJson.Deserialize(json);

        Assert.AreEqual(QuackRisk.High, frame.Risk);
    }

    [TestMethod]
    public void Deserialize_Risk_LongForm()
    {
        var json = "{\"version\":1,\"verb\":\"quack\",\"id\":\"01JQA1\",\"source\":\"test\",\"risk\":\"critical\"}";
        var frame = QuackJson.Deserialize(json);

        Assert.AreEqual(QuackRisk.Critical, frame.Risk);
    }

    [TestMethod]
    public void RoundTrip_AllFields_PreservesValues()
    {
        var original = QuackFrame.Egg(
            source: "planner",
            destination: "executor",
            eggId: "plan-1",
            digest: "sha256:abc123",
            context: "k8s/default",
            correlation: "corr-1",
            risk: QuackRisk.Medium,
            summary: "restart deployment",
            tone: QuackTone.SeriousDuck);

        var json = QuackJson.Serialize(original);
        var roundtripped = QuackJson.Deserialize(json);

        Assert.AreEqual(original.Version, roundtripped.Version);
        Assert.AreEqual(original.Verb, roundtripped.Verb);
        Assert.AreEqual(original.Id, roundtripped.Id);
        Assert.AreEqual(original.Source, roundtripped.Source);
        Assert.AreEqual(original.Destination, roundtripped.Destination);
        Assert.AreEqual(original.Context, roundtripped.Context);
        Assert.AreEqual(original.Correlation, roundtripped.Correlation);
        Assert.AreEqual(original.Risk, roundtripped.Risk);
        Assert.AreEqual(original.Summary, roundtripped.Summary);
        Assert.AreEqual(original.Digest, roundtripped.Digest);
        Assert.AreEqual(original.Tone, roundtripped.Tone);
    }

    [TestMethod]
    public void RoundTrip_Tone_PreservesValue()
    {
        var original = QuackFrame.Quack("test", tone: QuackTone.PlayfulDuck);
        var json = QuackJson.Serialize(original);
        var roundtripped = QuackJson.Deserialize(json);

        Assert.AreEqual(original.Tone, roundtripped.Tone);
    }

    [TestMethod]
    public void Deserialize_NullOptionalFields_AreNull()
    {
        var json = "{\"version\":1,\"verb\":\"quack\",\"id\":\"01JQA1\",\"source\":\"test\"}";
        var frame = QuackJson.Deserialize(json);

        Assert.IsNull(frame.Destination);
        Assert.IsNull(frame.Context);
        Assert.IsNull(frame.Correlation);
        Assert.IsNull(frame.Summary);
        Assert.IsNull(frame.Digest);
        Assert.IsNull(frame.Tone);
        Assert.IsNull(frame.Ttl);
    }

    [TestMethod]
    public void Deserialize_UnknownVerb_Throws()
    {
        var json = "{\"version\":1,\"verb\":\"tweet\",\"id\":\"01JQA1\",\"source\":\"test\"}";
        try { QuackJson.Deserialize(json); Assert.Fail("Expected JsonException"); } catch (JsonException) { }
    }

    [TestMethod]
    public void Serialize_All11Verbs_RoundTrip()
    {
        var verbs = Enum.GetValues<QuackVerb>();
        foreach (var verb in verbs)
        {
            var frame = verb switch
            {
                QuackVerb.Quack => QuackFrame.Quack("test"),
                QuackVerb.Honk => QuackFrame.Honk("test", "reason"),
                QuackVerb.Splash => QuackFrame.Splash("test", [new EvidenceRef { Kind = "log", Digest = "sha256:abc" }]),
                QuackVerb.Molt => QuackFrame.Molt("test", "corr-1"),
                QuackVerb.Peck => QuackFrame.Peck("test", "dst"),
                QuackVerb.Egg => QuackFrame.Egg("test", "dst", "egg-1", "sha256:abc"),
                QuackVerb.Hatch => QuackFrame.Hatch("test", "dst", "egg-1", "corr-1"),
                QuackVerb.Flap => QuackFrame.Flap("test", "dst", "egg-1", "sha256:abc", "corr-1"),
                QuackVerb.Bob => QuackFrame.Bob("test", "dst", "corr-1"),
                QuackVerb.Nack => QuackFrame.Nack("test", "dst", "corr-1"),
                QuackVerb.Perch => QuackFrame.Perch("test", "dst", "corr-1"),
                _ => throw new InvalidOperationException(),
            };

            var json = QuackJson.Serialize(frame);
            var roundtripped = QuackJson.Deserialize(json);

            Assert.AreEqual(verb, roundtripped.Verb, $"Round-trip failed for verb: {verb}");
        }
    }
}
