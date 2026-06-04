namespace Quack.Tests;

[TestClass]
public sealed class QuackRuleTests
{
    [TestMethod]
    public void QuackRule_ContainsExpectedRules()
    {
        var values = Enum.GetValues<QuackRule>();

        Assert.AreEqual(11, values.Length);
    }

    [TestMethod]
    public void QuackRule_Values_AreDistinct()
    {
        var values = Enum.GetValues<QuackRule>();
        var distinctValues = new HashSet<int>();

        foreach (var value in values)
        {
            Assert.IsTrue(
                distinctValues.Add((int)value),
                $"Duplicate QuackRule value: {(int)value}");
        }
    }
}
