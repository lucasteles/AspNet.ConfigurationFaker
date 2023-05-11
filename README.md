[![CI](https://github.com/lucasteles/AspNet.ConfigurationFaker/actions/workflows/ci.yml/badge.svg)](https://github.com/lucasteles/AspNet.ConfigurationFaker/actions/workflows/ci.yml)
[![Nuget](https://img.shields.io/nuget/v/AspNet.ConfigurationFaker.svg?style=flat)](https://www.nuget.org/packages/AspNet.ConfigurationFaker)
![](https://raw.githubusercontent.com/lucasteles/AspNet.ConfigurationFaker/badges/badge_linecoverage.svg)
![](https://raw.githubusercontent.com/lucasteles/AspNet.ConfigurationFaker/badges/badge_branchcoverage.svg)
![](https://raw.githubusercontent.com/lucasteles/AspNet.ConfigurationFaker/badges/test_report_badge.svg)
![](https://raw.githubusercontent.com/lucasteles/AspNet.ConfigurationFaker/badges/lines_badge.svg)

![](https://raw.githubusercontent.com/lucasteles/AspNet.ConfigurationFaker/badges/dotnet_version_badge.svg)
![](https://img.shields.io/badge/Lang-C%23-green)
![https://editorconfig.org/](https://img.shields.io/badge/style-EditorConfig-black)

# AspNet.ConfigurationFaker

Simple change AspNet Core configuration on integration tests

## Getting started

[NuGet package](https://www.nuget.org/packages/AspNet.ConfigurationFaker) available:

```ps
$ dotnet add package AspNet.ConfigurationFaker
```

## How To Use:

```csharp
class TestFixture : WebApplicationFactory<Program>
{
    FakeConfigurationProvider FakeConfig { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
        .ConfigureAppConfiguration(config =>
        {
            config.RemoveJsonSource("appsettings.Local.json");

            config.AddFakeConfiguration(FakeConfig);

            FakeConfig.ReplaceConfigurationUrls(config, "wiremock", "http://localhost:1234");
            // replaces any "http://wiremock" value on test config for http://localhost:1234

        })
        .ConfigureTestServices(services =>
        {
            /* ... */
        });

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        FakeConfig.Add("ApiKey", "TestKey");
        FakeConfig.Freeze();
    }

    [SetUp]
    public void Setup() =>
        FakeConfig.Reset(); // resets to the last frozen state

    [Test]
    public void ConfigTest()
    {
        FakeConfig.Add("Parent:ChildValue", 42);

        FakeConfig.AddJson(
            """
            {
              "Parent": {
                "ChildValue": 42
              }
            }
            """);

        FakeConfig.AddAsJson(new
        {
            Parent = new
            {
                ChildValue = 42,
            },
        });

        /* Code that injects IConfiguration or IOptions */
    }
}
);
```

