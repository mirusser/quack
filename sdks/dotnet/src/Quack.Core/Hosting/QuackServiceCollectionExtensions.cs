using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Quack;

/// <summary>
/// Extension methods for registering Quack services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class QuackServiceCollectionExtensions
{
    /// <summary>
    /// Adds Quack services to the service collection.
    /// Registers IQuackEmitter, IQuackValidator, IQuackSink, ITraceSink, and QuackOptions.
    /// </summary>
    public static IQuackBuilder AddQuack(
        this IServiceCollection services,
        Action<QuackOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new QuackOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.TryAddSingleton<IQuackValidator, QuackValidator>();
        services.TryAddSingleton<IQuackEmitter, QuackEmitter>();

        // Register IQuackSink as composite of all registered sinks
        services.TryAddSingleton<IQuackSink>(sp =>
        {
            var sinks = sp.GetServices<IQuackSink>().ToList();
            return sinks.Count switch
            {
                0 => new NoopQuackSink(),
                1 => sinks[0],
                _ => new CompositeQuackSink(sinks),
            };
        });

        // Register ITraceSink as composite of all registered trace sinks
        services.TryAddSingleton<ITraceSink>(sp =>
        {
            var sinks = sp.GetServices<ITraceSink>().ToList();
            return sinks.Count switch
            {
                0 => new LoggerTraceSink(sp.GetRequiredService<ILoggerFactory>().CreateLogger("Quack.Trace")),
                _ => new CompositeTraceSink(sinks),
            };
        });

        return new QuackBuilder(services);
    }
}
