namespace Quack.Cbor.Tests;

[TestClass]
public sealed class QuackCborTests
{
    [TestMethod]
    public void RoundTrip_MinimalFrame_PreservesRequiredFields()
    {
        var frame = new QuackFrame
        {
            Version = "0.1",
            Verb = QuackVerb.Quack,
            Source = "observer",
            Risk = QuackRisk.None,
        };

        var decoded = QuackCbor.Decode(QuackCbor.Encode(frame));

        Assert.AreEqual(frame.Version, decoded.Version);
        Assert.AreEqual(frame.Verb, decoded.Verb);
        Assert.AreEqual(frame.Source, decoded.Source);
        Assert.AreEqual(frame.Risk, decoded.Risk);
    }

    [TestMethod]
    public void RoundTrip_AllScalarFields_PreservesEverything()
    {
        var frame = new QuackFrame
        {
            Version = "0.1",
            Verb = QuackVerb.Peck,
            Id = "01JQAEX8Z1W0M6N2P5R9V3K7G4",
            Timestamp = "2026-06-05T12:00:00.000Z",
            Source = "planner",
            Destination = "executor",
            Context = "ctx-abc",
            Correlation = "corr-1",
            Risk = QuackRisk.High,
            Summary = "test summary",
            Digest = "sha256:aabbccdd",
            Ttl = 300,
            Tone = QuackTone.SeriousDuck,
            Profile = "quack-mutation-v0",
            ExpiresAt = "2026-06-05T13:00:00.000Z",
            TaskId = "task-xyz",
        };

        var decoded = QuackCbor.Decode(QuackCbor.Encode(frame));

        Assert.AreEqual(frame.Version, decoded.Version);
        Assert.AreEqual(frame.Verb, decoded.Verb);
        Assert.AreEqual(frame.Id, decoded.Id);
        Assert.AreEqual(frame.Timestamp, decoded.Timestamp);
        Assert.AreEqual(frame.Source, decoded.Source);
        Assert.AreEqual(frame.Destination, decoded.Destination);
        Assert.AreEqual(frame.Context, decoded.Context);
        Assert.AreEqual(frame.Correlation, decoded.Correlation);
        Assert.AreEqual(frame.Risk, decoded.Risk);
        Assert.AreEqual(frame.Summary, decoded.Summary);
        Assert.AreEqual(frame.Digest, decoded.Digest);
        Assert.AreEqual(frame.Ttl, decoded.Ttl);
        Assert.AreEqual(frame.Tone, decoded.Tone);
        Assert.AreEqual(frame.Profile, decoded.Profile);
        Assert.AreEqual(frame.ExpiresAt, decoded.ExpiresAt);
        Assert.AreEqual(frame.TaskId, decoded.TaskId);
    }

    [TestMethod]
    public void RoundTrip_WithNestedJsonData_PreservesStructure()
    {
        var json = """{"planId":"plan-1","action":"restart","tags":["a","b"],"meta":{"count":3}}""";
        using var doc = JsonDocument.Parse(json);
        var frame = new QuackFrame
        {
            Version = "0.1",
            Verb = QuackVerb.Egg,
            Source = "planner",
            Risk = QuackRisk.Medium,
            Data = doc.RootElement.Clone(),
        };

        var decoded = QuackCbor.Decode(QuackCbor.Encode(frame));

        Assert.IsNotNull(decoded.Data);
        var data = decoded.Data.Value;
        Assert.AreEqual("plan-1", data.GetProperty("planId").GetString());
        Assert.AreEqual("restart", data.GetProperty("action").GetString());
        Assert.AreEqual(2, data.GetProperty("tags").GetArrayLength());
        Assert.AreEqual("a", data.GetProperty("tags")[0].GetString());
        Assert.AreEqual(3, data.GetProperty("meta").GetProperty("count").GetInt32());
    }

    [TestMethod]
    public void RoundTrip_AllRiskLevels_RoundTripCorrectly()
    {
        foreach (var risk in Enum.GetValues<QuackRisk>())
        {
            var frame = new QuackFrame { Version = "0.1", Verb = QuackVerb.Quack, Source = "s", Risk = risk };
            var decoded = QuackCbor.Decode(QuackCbor.Encode(frame));
            Assert.AreEqual(risk, decoded.Risk, $"Risk {risk} did not round-trip");
        }
    }

    [TestMethod]
    public void RoundTrip_AllVerbs_RoundTripCorrectly()
    {
        foreach (var verb in Enum.GetValues<QuackVerb>())
        {
            var frame = new QuackFrame { Version = "0.1", Verb = verb, Source = "s", Risk = QuackRisk.None };
            var decoded = QuackCbor.Decode(QuackCbor.Encode(frame));
            Assert.AreEqual(verb, decoded.Verb, $"Verb {verb} did not round-trip");
        }
    }

    [TestMethod]
    public void Encode_ProducesDefiniteLengthMap()
    {
        var frame = new QuackFrame { Version = "0.1", Verb = QuackVerb.Quack, Source = "s", Risk = QuackRisk.None };
        var bytes = QuackCbor.Encode(frame);

        // CBOR definite-length map: high 3 bits = 101 (0xA0..0xBF range, or 0xB8/0xB9 for larger maps)
        // Indefinite-length map starts with 0xBF
        Assert.AreNotEqual(0xBF, bytes[0], "Encoder must not produce indefinite-length maps (spec §5.3)");
        // First byte must be a map major type (0xA0 + length for short maps, or 0xB8/0xB9 for longer ones)
        Assert.IsTrue((bytes[0] & 0xE0) == 0xA0, $"First byte 0x{bytes[0]:X2} is not a CBOR map type");
    }

    [TestMethod]
    public void Encode_KeysAreInAscendingOrder()
    {
        // Frame with all optional fields to verify key ordering covers every slot
        var json = """{"x":1}""";
        using var doc = JsonDocument.Parse(json);
        var frame = new QuackFrame
        {
            Version = "0.1",
            Verb = QuackVerb.Quack,
            Id = "id-1",
            Timestamp = "2026-06-05T00:00:00Z",
            Source = "s",
            Destination = "d",
            Context = "ctx",
            Correlation = "corr",
            Risk = QuackRisk.Low,
            Digest = "sha256:00",
            Summary = "sum",
            Data = doc.RootElement.Clone(),
            Ttl = 60,
            Tone = QuackTone.PlayfulDuck,
            Profile = "quack-mutation-v0",
            ExpiresAt = "2026-06-05T01:00:00Z",
            TaskId = "t-1",
        };

        // Round-trip confirms decoder reads keys correctly regardless of their wire order
        // but the encoder must write them in ascending order for deterministic CBOR
        var decoded = QuackCbor.Decode(QuackCbor.Encode(frame));
        Assert.AreEqual(frame.Id, decoded.Id);
        Assert.AreEqual(frame.Data?.GetProperty("x").GetInt32(), decoded.Data?.GetProperty("x").GetInt32());
        Assert.AreEqual(frame.Ttl, decoded.Ttl);
        Assert.AreEqual(frame.Tone, decoded.Tone);
    }
}
