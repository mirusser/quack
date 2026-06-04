namespace Quack.Tests;

[TestClass]
public sealed class EvidenceRefTests
{
    [TestMethod]
    public void EvidenceRef_DefaultValues_AreEmptyOrNull()
    {
        var evidence = CreateEvidenceRef();

        Assert.AreEqual(string.Empty, GetProperty<string>(evidence, "Kind"));
        Assert.AreEqual(string.Empty, GetProperty<string>(evidence, "Digest"));
        Assert.IsNull(GetProperty<string>(evidence, "Uri"));
        Assert.IsNull(GetProperty<string>(evidence, "MediaType"));
    }

    [TestMethod]
    public void EvidenceRef_WithValues_PropertiesAreSet()
    {
        var evidence = CreateEvidenceRef();
        SetProperty(evidence, "Kind", "k8s.events");
        SetProperty(evidence, "Digest", "sha256:abc123");
        SetProperty(evidence, "Uri", "artifact://events/default/web");
        SetProperty(evidence, "MediaType", "application/json");

        Assert.AreEqual("k8s.events", GetProperty<string>(evidence, "Kind"));
        Assert.AreEqual("sha256:abc123", GetProperty<string>(evidence, "Digest"));
        Assert.AreEqual("artifact://events/default/web", GetProperty<string>(evidence, "Uri"));
        Assert.AreEqual("application/json", GetProperty<string>(evidence, "MediaType"));
    }

    private static object CreateEvidenceRef()
    {
        var type = Type.GetType("Quack.QuackFrame+EvidenceRef, Quack.Core")
            ?? Type.GetType("Quack.EvidenceRef, Quack.Core")
            ?? throw new InvalidOperationException("EvidenceRef type was not found.");

        return Activator.CreateInstance(type)!;
    }

    private static T? GetProperty<T>(object instance, string propertyName) =>
        (T?)instance.GetType().GetProperty(propertyName)!.GetValue(instance);

    private static void SetProperty(object instance, string propertyName, object value) =>
        instance.GetType().GetProperty(propertyName)!.SetValue(instance, value);
}
