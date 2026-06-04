namespace Quack.Testing.Tests;

[TestClass]
public sealed class GoldenFixtureTests
{
    [TestMethod]
    public void AllValidFixtures_DeserializeAndValidate()
    {
        var fixturesPath = Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../../../spec/fixtures");

        var validFixtures = GoldenFixtureRunner.LoadValidFixtures(fixturesPath);
        Assert.IsTrue(validFixtures.Count > 0, "No valid fixtures found");
    }

    [TestMethod]
    public void AllInvalidFixtures_FailValidationOrDeserialization()
    {
        var fixturesPath = Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../../../spec/fixtures");

        var unexpectedlyValid = GoldenFixtureRunner.LoadInvalidFixtures(fixturesPath);
        Assert.AreEqual(0, unexpectedlyValid.Count,
            $"These invalid fixtures unexpectedly passed: {string.Join(", ", unexpectedlyValid)}");
    }
}
