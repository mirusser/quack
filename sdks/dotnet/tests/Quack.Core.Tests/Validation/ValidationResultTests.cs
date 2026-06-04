namespace Quack.Tests;

[TestClass]
public sealed class ValidationResultTests
{
    [TestMethod]
    public void Valid_WhenCreated_HasNoErrors()
    {
        var result = Quack.ValidationResult.Valid();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Invalid_WithErrors_ExposesErrors()
    {
        var firstError = new QuackError("FIRST", "first", QuackRule.InvalidFrame);
        var secondError = new QuackError("SECOND", "second", QuackRule.TtlExceedsMax);

        var result = Quack.ValidationResult.Invalid(firstError, secondError);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(2, result.Errors.Count);
    }
}
