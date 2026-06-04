namespace Quack.Tests;

[TestClass]
public sealed class QuackIdTests
{
    private const string CrockfordBase32Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    [TestMethod]
    public void NewId_WhenCalled_ReturnsCrockfordBase32Ulid()
    {
        var id = QuackId.NewId();

        Assert.AreEqual(26, id.Length);
        foreach (var character in id)
        {
            Assert.IsTrue(
                CrockfordBase32Alphabet.Contains(character),
                $"Unexpected ULID character: {character}");
        }
    }

    [TestMethod]
    public void NewId_MultipleCalls_AreLexicallyIncreasing()
    {
        var previous = QuackId.NewId();

        for (var i = 0; i < 100; i++)
        {
            var current = QuackId.NewId();

            Assert.IsTrue(
                string.CompareOrdinal(current, previous) >= 0,
                $"Expected {current} to be lexically greater than or equal to {previous}.");

            previous = current;
        }
    }
}
