namespace Quack;

/// <summary>
/// Fluent builder for configuring the Quack sink and trace pipelines.
/// </summary>
public interface IQuackBuilder
{
    /// <summary>The service collection being configured.</summary>
    IServiceCollection Services { get; }

    /// <summary>Register a delivery sink.</summary>
    IQuackBuilder AddSink<T>() where T : class, IQuackSink;

    /// <summary>Register a trace sink.</summary>
    IQuackBuilder AddTraceSink<T>() where T : class, ITraceSink;
}

internal sealed class QuackBuilder(IServiceCollection services) : IQuackBuilder
{
    public IServiceCollection Services { get; } = services;

    public IQuackBuilder AddSink<T>() where T : class, IQuackSink
    {
        Services.TryAddSingleton<T>();
        Services.AddSingleton(new QuackSinkRegistration(typeof(T)));
        return this;
    }

    public IQuackBuilder AddTraceSink<T>() where T : class, ITraceSink
    {
        Services.TryAddSingleton<T>();
        Services.AddSingleton(new QuackTraceSinkRegistration(typeof(T)));
        return this;
    }
}

internal sealed record QuackSinkRegistration(Type SinkType);

internal sealed record QuackTraceSinkRegistration(Type SinkType);
