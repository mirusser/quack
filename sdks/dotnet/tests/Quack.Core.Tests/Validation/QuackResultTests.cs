namespace Quack.Tests;

[TestClass]
public sealed class QuackResultTests
{
    [TestMethod]
    public void Success_WithFrame_SetsFrameAndNoErrors()
    {
        var frame = QuackFrame.Quack("observer");

        var result = QuackResult.Success(frame);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.IsRejected);
        Assert.IsNotNull(result.Frame);
        Assert.AreSame(frame, result.Frame);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Rejected_WithErrors_HasNullFrameAndErrors()
    {
        var firstError = new QuackError("FIRST", "first", QuackRule.InvalidFrame);
        var secondError = new QuackError("SECOND", "second", QuackRule.RiskExceedsMax);

        var result = QuackResult.Rejected(firstError, secondError);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.IsRejected);
        Assert.IsNull(result.Frame);
        Assert.AreEqual(2, result.Errors.Count);
    }
}
