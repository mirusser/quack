namespace Quack.Tests;

[TestClass]
public sealed class QuackErrorTests
{
    [TestMethod]
    public void Constructor_WithValues_SetsTypedProperties()
    {
        var error = new QuackError("CODE", "msg", QuackRule.InvalidFrame);

        Assert.AreEqual("CODE", error.Code);
        Assert.AreEqual("msg", error.Message);
        Assert.AreEqual(QuackRule.InvalidFrame, error.Rule);
    }
}
