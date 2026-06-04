using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Quack.Tests;

[TestClass]
public sealed class QuackServiceCollectionExtensionsTests
{
    [TestMethod]
    public void AddQuack_WithNoSinks_ResolvesNoopSink()
    {
        var services = new ServiceCollection();

        services.AddQuack();
        using var provider = services.BuildServiceProvider();

        var sink = provider.GetRequiredService<IQuackSink>();

        Assert.IsInstanceOfType<NoopQuackSink>(sink);
    }

    [TestMethod]
    public void AddQuack_WithOptions_ConfiguresSingletonOptions()
    {
        var services = new ServiceCollection();

        services.AddQuack(options =>
        {
            options.AgentName = "test-agent";
            options.MaxRisk = QuackRisk.Medium;
            options.MaxTtl = 500;
            options.MaxQuackVersion = 2;
        });
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<QuackOptions>();
        var secondOptions = provider.GetRequiredService<QuackOptions>();

        Assert.AreSame(options, secondOptions);
        Assert.AreEqual("test-agent", options.AgentName);
        Assert.AreEqual(QuackRisk.Medium, options.MaxRisk);
        Assert.AreEqual(500, options.MaxTtl);
        Assert.AreEqual(2, options.MaxQuackVersion);
    }

    [TestMethod]
    public void AddQuack_ResolvesEmitter()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddQuack();
        using var provider = services.BuildServiceProvider();

        var emitter = provider.GetRequiredService<IQuackEmitter>();

        Assert.IsNotNull(emitter);
    }

    [TestMethod]
    public void AddQuack_ResolvesValidator()
    {
        var services = new ServiceCollection();

        services.AddQuack();
        using var provider = services.BuildServiceProvider();

        var validator = provider.GetRequiredService<IQuackValidator>();

        Assert.IsInstanceOfType<QuackValidator>(validator);
    }

    [TestMethod]
    public void AddQuack_WithNullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;

        Assert.ThrowsExactly<ArgumentNullException>(() => services!.AddQuack());
    }

    [TestMethod]
    public void AddQuack_ReturnsBuilderWithServiceCollection()
    {
        var services = new ServiceCollection();

        var builder = services.AddQuack();

        Assert.IsNotNull(builder);
        Assert.IsInstanceOfType<IQuackBuilder>(builder);
        Assert.AreSame(services, builder.Services);
    }

    [TestMethod]
    public void AddSink_WithSinkType_RegistersSinkInServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddQuack().AddSink<NoopQuackSink>();
        using var provider = services.BuildServiceProvider();

        var sink = provider.GetRequiredService<IQuackSink>();

        Assert.IsInstanceOfType<NoopQuackSink>(sink);
    }

    [TestMethod]
    public void AddTraceSink_WithTraceSinkType_RegistersTraceSink()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger("Quack.Tests"));

        services.AddQuack().AddTraceSink<LoggerTraceSink>();
        using var provider = services.BuildServiceProvider();

        var sink = provider.GetRequiredService<ITraceSink>();

        Assert.IsNotNull(sink);
    }
}
