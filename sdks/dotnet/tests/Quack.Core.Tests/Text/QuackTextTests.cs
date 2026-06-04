namespace Quack.Tests;

[TestClass]
public sealed class QuackTextTests
{
    [TestMethod]
    public void Encode_MinimalFrame_ProducesExpectedFormat()
    {
        var frame = new QuackFrame
        {
            Version = "0.1",
            Verb = QuackVerb.Quack,
            Id = "01JQA1X5Z8W7M3N9P2R6V0K4B1",
            Source = "observer",
        };

        var text = QuackText.Encode(frame);

        Assert.IsTrue(text.StartsWith("QK1 quack"));
        Assert.IsTrue(text.Contains("quackId=01JQA1X5Z8W7M3N9P2R6V0K4B1"));
        Assert.IsTrue(text.Contains("source=observer"));
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
        Assert.IsTrue(text.Contains("summary=\"hello world\""));
    }

    [TestMethod]
    public void Encode_WithDigestTtlTone_IncludesExpectedKeys()
    {
        var frame = QuackFrame.Quack("observer") with
        {
            Digest = "sha256:abc",
            Ttl = 1000,
            Tone = QuackTone.SeriousDuck,
        };

        var text = QuackText.Encode(frame);

        Assert.IsTrue(text.Contains("digest=sha256:abc"));
        Assert.IsTrue(text.Contains("ttl=1000"));
        Assert.IsTrue(text.Contains("tone=serious-duck"));
    }

    [TestMethod]
    public void Encode_WithNullOptionalFields_OmitsOptionalKeys()
    {
        var frame = new QuackFrame
        {
            Version = "0.1",
            Verb = QuackVerb.Quack,
            Id = "01JQA1X5Z8W7M3N9P2R6V0K4B1",
            Source = "observer",
        };

        var text = QuackText.Encode(frame);

        Assert.IsFalse(text.Contains("destination="));
        Assert.IsFalse(text.Contains("context="));
        Assert.IsFalse(text.Contains("correlation="));
        Assert.IsFalse(text.Contains("summary="));
        Assert.IsFalse(text.Contains("digest="));
        Assert.IsFalse(text.Contains("ttl="));
        Assert.IsFalse(text.Contains("tone="));
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

    [TestMethod]
    public void EncodeValue_WithQuoteAndBackslash_EscapesValue()
    {
        var result = QuackText.EncodeValue("he\"llo\\world");

        Assert.AreEqual("\"he\\\"llo\\\\world\"", result);
    }

    [TestMethod]
    public void Decode_RoundTrip_MinimalFrame()
    {
        var original = new QuackFrame
        {
            Version = "0.1",
            Verb = QuackVerb.Quack,
            Id = "01JQA1X5Z8W7M3N9P2R6V0K4B1",
            Source = "observer",
        };
        var text = QuackText.Encode(original);
        var decoded = QuackText.Decode(text);

        Assert.AreEqual(original.Verb, decoded.Verb);
        Assert.AreEqual(original.Id, decoded.Id);
        Assert.AreEqual(original.Source, decoded.Source);
        Assert.AreEqual("0.1", decoded.Version);
    }

    [TestMethod]
    public void Decode_RoundTrip_WithAllFields()
    {
        var original = QuackFrame.Quack(
            source: "observer",
            destination: "listener",
            context: "k8s/default",
            correlation: "corr-1",
            risk: QuackRisk.Medium,
            summary: "deployment unavailable",
            tone: QuackTone.SeriousDuck);

        var text = QuackText.Encode(original);
        var decoded = QuackText.Decode(text);

        Assert.AreEqual(original.Verb, decoded.Verb);
        Assert.AreEqual(original.Id, decoded.Id);
        Assert.AreEqual(original.Source, decoded.Source);
        Assert.AreEqual(original.Destination, decoded.Destination);
        Assert.AreEqual(original.Context, decoded.Context);
        Assert.AreEqual(original.Correlation, decoded.Correlation);
        Assert.AreEqual(original.Risk, decoded.Risk);
        Assert.AreEqual(original.Summary, decoded.Summary);
        Assert.AreEqual(original.Tone, decoded.Tone);
    }

    [TestMethod]
    public void Decode_RoundTrip_WithDigestAndTtl()
    {
        var original = QuackFrame.Quack("observer") with
        {
            Digest = "sha256:abc123",
            Ttl = 300000,
        };
        var text = QuackText.Encode(original);
        var decoded = QuackText.Decode(text);

        Assert.AreEqual(original.Digest, decoded.Digest);
        Assert.AreEqual(original.Ttl, decoded.Ttl);
    }

    [TestMethod]
    public void Decode_RoundTrip_WithTimestamp()
    {
        var original = QuackFrame.Quack("observer") with
        {
            Timestamp = "2026-06-05T12:00:00.000Z",
        };
        var text = QuackText.Encode(original);
        var decoded = QuackText.Decode(text);

        Assert.AreEqual(original.Timestamp, decoded.Timestamp);
    }
}
