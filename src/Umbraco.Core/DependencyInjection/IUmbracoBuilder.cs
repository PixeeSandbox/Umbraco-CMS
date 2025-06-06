using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Logging;

namespace Umbraco.Cms.Core.DependencyInjection;

public interface IUmbracoBuilder
{
    IServiceCollection Services { get; }

    IConfiguration Config { get; }

    TypeLoader TypeLoader { get; }

    /// <summary>
    ///     A Logger factory created specifically for the <see cref="IUmbracoBuilder" />. This is NOT the same
    ///     instance that will be resolved from DI. Use only if required during configuration.
    /// </summary>
    ILoggerFactory BuilderLoggerFactory { get; }

    IProfiler Profiler { get; }

    AppCaches AppCaches { get; }

    TBuilder WithCollectionBuilder<TBuilder>()
        where TBuilder : ICollectionBuilder;

    void Build();
}
