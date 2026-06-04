namespace Quack.Tests;

[TestClass]
public sealed class QuackOptionsTests
{
    [TestMethod]
    public void QuackOptions_DefaultValues_MatchProtocolDefaults()
    {
        var options = new QuackOptions();

        Assert.AreEqual("quack-agent", options.AgentName);
        Assert.AreEqual(QuackRisk.High, options.MaxRisk);
        Assert.IsNull(options.MaxTtl);
        Assert.AreEqual("0.1", options.MaxQuackVersion);
    }

    [TestMethod]
    public void QuackOptions_Setters_UpdateValues()
    {
        var options = new QuackOptions
        {
            AgentName = "configured-agent",
            MaxRisk = QuackRisk.Low,
            MaxTtl = 250,
            MaxQuackVersion = "2.0",
        };

        Assert.AreEqual("configured-agent", options.AgentName);
        Assert.AreEqual(QuackRisk.Low, options.MaxRisk);
        Assert.AreEqual(250, options.MaxTtl);
        Assert.AreEqual("2.0", options.MaxQuackVersion);
    }
}
