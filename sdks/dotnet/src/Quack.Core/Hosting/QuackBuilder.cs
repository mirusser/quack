using Microsoft.Extensions.DependencyInjection;

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

internal sealed class QuackBuilder : IQuackBuilder
{
    private readonly List<Type> _sinkTypes = new();
    private readonly List<Type> _traceSinkTypes = new();

    public QuackBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public IQuackBuilder AddSink<T>() where T : class, IQuackSink
    {
        _sinkTypes.Add(typeof(T));
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<IQuackSink, T>());
        return this;
    }

    public IQuackBuilder AddTraceSink<T>() where T : class, ITraceSink
    {
        _traceSinkTypes.Add(typeof(T));
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITraceSink, T>());
        return this;
    }
}
