using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace AspNet.ConfigurationFaker;

/// <summary>
/// Fake InMemory ConfigurationSource
/// </summary>
public sealed class FakeConfigurationSource : IConfigurationSource
{
    readonly FakeConfigurationProvider provider;

    /// <summary>
    /// Create new Fake Configuration Source
    /// </summary>
    public FakeConfigurationSource() => provider = new();

    /// <summary>
    /// Create new Fake Configuration Source from a Fake Configuration Provider
    /// </summary>
    public FakeConfigurationSource(FakeConfigurationProvider provider) =>
        this.provider = provider;

    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder) => provider;

    /// <summary>
    /// Returns the provider instance
    /// </summary>
    public FakeConfigurationProvider Get() => provider;
}

/// <summary>
/// Fake InMemory ConfigurationProvider
/// </summary>
public sealed class FakeConfigurationProvider :
    ConfigurationProvider,
    IEnumerable<KeyValuePair<string, string?>>
{
    ImmutableDictionary<string, string?>? cache;

    /// <summary>
    /// Return true if key exists on configuration
    /// </summary>
    public bool ContainsKey(string key) => Data.ContainsKey(key);

    /// <summary>
    /// Remove configuration by key
    /// </summary>
    public bool Remove(string key)
    {
        var result = Data.Remove(key);
        base.OnReload();
        return result;
    }

    /// <summary>
    ///  Add key/value to configuration
    /// </summary>
    public void Add(string key, object? value)

    {
        this[key] = value?.ToString();
        base.OnReload();
    }

    /// <summary>
    /// Add Dictionary values to configuration
    /// </summary>
    public void Add(Dictionary<string, string?> items)
    {
        AddRange(items);
        base.OnReload();
    }

    /// <summary>
    /// Add pair values to configuration
    /// </summary>
    public void AddRange(IEnumerable<KeyValuePair<string, string?>> items)
    {
        foreach (var (key, value) in items)
            Add(key, value);

        base.OnReload();
    }

    /// <summary>
    /// Freezes configuration state for reset
    /// </summary>
    public void Freeze() => cache = Data.ToImmutableDictionary();

    /// <summary>
    /// Reset configuration to last frozen state
    /// </summary>
    public void Reset()
    {
        Data.Clear();
        if (cache is not null)
            AddRange(cache);
    }

    /// <summary>
    /// Add object serialized as json on configuration on specified configuration path
    /// </summary>
    public void AddAsJson(object obj, string? path = null, JsonSerializerOptions? options = null)
    {
        var json = JsonSerializer.Serialize(obj, options);
        AddJson(json, path);
    }

    /// <summary>
    /// Add json as configuration on specified configuration path
    /// </summary>
#if NET7_0_OR_GREATER
    public void AddJson([StringSyntax(StringSyntaxAttribute.Json)] string json, string? path = null)
#else
    public void AddJson(string json, string? path = null)
#endif
    {
        ArgumentNullException.ThrowIfNull(json);
        var jsonData =
            new ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(Encoding.ASCII.GetBytes(json)))
                .Build()
                .AsEnumerable()
                .ToDictionary(x => string.IsNullOrWhiteSpace(path)
                        ? x.Key
                        : $"{path}:{x.Key}",
                    x => x.Value);

        AddRange(jsonData!);
    }

    /// <summary>
    /// Read/Create/Replace configuration key
    /// </summary>
    public string? this[string key]
    {
        get => Data[key];
        set
        {
            if (Data.ContainsKey(key))
                Set(key, value);
            else
                Data.Add(key, value);
        }
    }

    /// <summary>
    /// Add ConnectionString Configuration
    /// </summary>
    /// <param name="connectionString">Connection string value</param>
    /// <param name="connectionName">Connection name (defaults to "DefaultConnection")</param>
    public void AddConnectionString(
        string connectionString,
        string connectionName = "DefaultConnection") =>
        Add($"ConnectionStrings:{connectionName}", connectionString);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() =>
        Data.GetEnumerator();

    /// <summary>
    /// Replace all configured url hosts
    /// </summary>
    /// <param name="configuration">The WebApp IConfiguration</param>
    /// <param name="host">The value that will be checked if contained in the url for replacing</param>
    /// <param name="replaceUri">Url to replace</param>
    public void ReplaceConfigurationUrls(
        IConfiguration configuration,
        string host,
        Uri replaceUri
    ) => ReplaceConfigurationUrls(configuration, host, replaceUri.ToString());

    /// <summary>
    /// Replace all configured url hosts
    /// </summary>
    /// <param name="configuration">The WebApp IConfiguration</param>
    /// <param name="host">The value that will be checked if contained in the url for replacing</param>
    /// <param name="replaceUrl">Url to replace</param>
    public void ReplaceConfigurationUrls(
        IConfiguration configuration,
        string host,
        string replaceUrl
    )
    {
        foreach (var (key, value) in configuration.AsEnumerable())
        {
            if (value is null || !value.StartsWith("http"))
                continue;

            var uri = new Uri(value);
            if (!uri.Host.ToLower().Contains(host))
                continue;

            var mockUri = new Uri(replaceUrl);
            var builder =
                new UriBuilder(uri)
                {
                    Host = mockUri.Host,
                    Scheme = mockUri.Scheme,
                    Port = mockUri.Port,
                };

            this.Add(key, builder.Uri.ToString());
        }
    }
}
