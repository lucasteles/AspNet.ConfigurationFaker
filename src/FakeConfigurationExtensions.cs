using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace AspNet.ConfigurationFaker;

/// <summary>
/// Fake Configuration Extensions
/// </summary>
public static class FakeConfigurationExtensions
{
    /// <summary>
    /// Add Fake Configuration Source
    /// </summary>
    /// <returns>The provider used to fake configuration</returns>
    public static FakeConfigurationProvider AddFakeConfiguration(
        this IConfigurationBuilder builder)
    {
        var source = new FakeConfigurationSource();
        builder.Add(source);
        return source.Get();
    }

    /// <summary>
    /// Add Fake Configuration Source from a already created provider
    /// </summary>
    /// <returns>The provider used to fake configuration</returns>
    public static IConfigurationBuilder AddFakeConfiguration(
        this IConfigurationBuilder builder, FakeConfigurationProvider provider)
    {
        var source = new FakeConfigurationSource(provider);
        builder.Add(source);
        return builder;
    }

    /// <summary>
    /// Remove all the configuration sources of type T which matches predicate
    /// </summary>
    public static IConfigurationBuilder RemoveSource<T>(
        this IConfigurationBuilder builder, Func<T, bool> predicate)
        where T : IConfigurationSource
    {
        builder.Sources
            .Where(s => s is T source && predicate(source))
            .ToList()
            .ForEach(s => builder.Sources.Remove(s));

        return builder;
    }

    /// <summary>
    /// Remove all JsonConfigurationSource with specified path
    /// </summary>
    public static IConfigurationBuilder RemoveJsonSource(
        this IConfigurationBuilder builder, string path) =>
        builder.RemoveSource<JsonConfigurationSource>(x =>
            x.Path?.ToLower() == path.ToLower());

    /// <summary>
    /// Apply each key value of the configuration to IWebHostBuilder
    /// </summary>
    public static IWebHostBuilder UseFakeConfigurationProvider(
        this IWebHostBuilder hostBuilder,
        FakeConfigurationProvider configuration)
    {
        foreach (var setting in configuration)
            hostBuilder.UseSetting(setting.Key, setting.Value);

        return hostBuilder;
    }
}
