namespace Quack.Tests;

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
            Version = 1,
            Verb = QuackVerb.Quack,
            Id = "01JQA1X5Z8W7M3N9P2R6V0K4B1",
            Source = "observer",
        };

        var text = QuackText.Encode(frame);

        Assert.IsFalse(text.Contains("destination="));
        Assert.IsFalse(text.Contains("context="));
        Assert.IsFalse(text.Contains("correlation="));
        Assert.IsFalse(text.Contains("say="));
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
}
