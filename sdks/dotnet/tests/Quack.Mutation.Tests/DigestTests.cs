namespace Quack.Mutation.Tests;

[TestClass]
public sealed class DigestTests
{
    [TestMethod]
    public void Compute_IdenticalJson_ProducesIdenticalDigest()
    {
        var json1 = "{\"b\":2,\"a\":1}";
        var json2 = "{\"a\":1,\"b\":2}";

        var digest1 = QuackDigest.Compute(json1);
        var digest2 = QuackDigest.Compute(json2);

        Assert.AreEqual(digest1, digest2);
    }

    [TestMethod]
    public void Compute_Format_IsSha256Colon64Hex()
    {
        var digest = QuackDigest.Compute("{}");

        Assert.IsTrue(digest.StartsWith("sha256:"));
        Assert.AreEqual(71, digest.Length); // "sha256:" + 64 hex chars
    }

    [TestMethod]
    public void Compute_Deterministic_SameInputSameOutput()
    {
        var input = "{\"key\":\"value\"}";

        var d1 = QuackDigest.Compute(input);
        var d2 = QuackDigest.Compute(input);

        Assert.AreEqual(d1, d2);
    }

    [TestMethod]
    public void Compute_DifferentInputs_DifferentDigests()
    {
        var d1 = QuackDigest.Compute("{\"x\":1}");
        var d2 = QuackDigest.Compute("{\"x\":2}");

        Assert.AreNotEqual(d1, d2);
    }

    [TestMethod]
    public void Validate_ValidDigest_ReturnsTrue()
    {
        Assert.IsTrue(QuackDigest.ValidateFormat("sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"));
    }

    [TestMethod]
    public void Validate_BadPrefix_ReturnsFalse()
    {
        Assert.IsFalse(QuackDigest.ValidateFormat("sha512:abc"));
    }

    [TestMethod]
    public void Validate_WrongLength_ReturnsFalse()
    {
        Assert.IsFalse(QuackDigest.ValidateFormat("sha256:abc"));
    }

    [TestMethod]
    public void Validate_Null_ReturnsFalse()
    {
        Assert.IsFalse(QuackDigest.ValidateFormat(null!));
    }

    [TestMethod]
    public void JsonCanonicalize_NormalizesKeyOrder()
    {
        var result = QuackDigest.JsonCanonicalize("{\"b\":2,\"a\":1}");
        Assert.AreEqual("{\"a\":1,\"b\":2}", result);
    }

    [TestMethod]
    public void JsonCanonicalize_StripsWhitespace()
    {
        var result = QuackDigest.JsonCanonicalize("{ \"a\" : 1 , \"b\" : 2 }");
        Assert.AreEqual("{\"a\":1,\"b\":2}", result);
    }
}
