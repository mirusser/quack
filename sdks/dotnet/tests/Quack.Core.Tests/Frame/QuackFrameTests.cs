namespace Quack.Tests;

[TestClass]
public sealed class QuackFrameTests
{
    [TestMethod]
    public void QuackFrame_DefaultValues_AreSensible()
    {
        var frame = new QuackFrame();

        Assert.AreEqual(0, frame.Version);
        Assert.AreEqual(default(QuackVerb), frame.Verb);
        Assert.AreEqual(string.Empty, frame.Id);
        Assert.IsNull(frame.Timestamp);
        Assert.AreEqual(string.Empty, frame.Source);
        Assert.IsNull(frame.Destination);
        Assert.IsNull(frame.Context);
        Assert.IsNull(frame.Correlation);
        Assert.AreEqual(QuackRisk.None, frame.Risk);
        Assert.IsNull(frame.Summary);
        Assert.IsNull(frame.Digest);
        Assert.IsNull(frame.Ttl);
        Assert.IsNull(frame.Tone);
        Assert.IsNull(frame.Data);
    }

    [TestMethod]
    public void QuackFrame_WithExpressions_CreatesImmutableCopy()
    {
        var original = new QuackFrame
        {
            Version = 1,
            Verb = QuackVerb.Quack,
            Id = "01JQA1X5Z8W7M3N9P2R6V0K4B1",
            Source = "observer",
            Summary = "hello",
            Risk = QuackRisk.Low,
        };

        var updated = original with { Summary = "updated" };

        Assert.AreEqual("hello", original.Summary);
        Assert.AreEqual("updated", updated.Summary);
        Assert.AreEqual(original.Version, updated.Version);
        Assert.AreEqual(original.Id, updated.Id);
    }
}

[TestClass]
public sealed class QuackVerbTests
{
    [TestMethod]
    public void QuackVerb_Has11Members()
    {
        var values = Enum.GetValues<QuackVerb>();
        Assert.AreEqual(11, values.Length);
    }
}

[TestClass]
public sealed class QuackRiskTests
{
    [TestMethod]
    public void QuackRisk_Values_MapCorrectly()
    {
        Assert.AreEqual(0, (int)QuackRisk.None);
        Assert.AreEqual(1, (int)QuackRisk.Low);
        Assert.AreEqual(2, (int)QuackRisk.Medium);
        Assert.AreEqual(3, (int)QuackRisk.High);
        Assert.AreEqual(4, (int)QuackRisk.Critical);
    }

    [TestMethod]
    public void QuackRisk_IsOrdered()
    {
        Assert.IsTrue(QuackRisk.None < QuackRisk.Low);
        Assert.IsTrue(QuackRisk.Low < QuackRisk.Medium);
        Assert.IsTrue(QuackRisk.Medium < QuackRisk.High);
        Assert.IsTrue(QuackRisk.High < QuackRisk.Critical);
    }
}

[TestClass]
public sealed class QuackToneTests
{
    [TestMethod]
    public void QuackTone_Has4Members()
    {
        var values = Enum.GetValues<QuackTone>();
        Assert.AreEqual(4, values.Length);
    }
}
