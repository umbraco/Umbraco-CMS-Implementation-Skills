using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.Runtime;
using Umbraco.Cms.Infrastructure.Runtime.RuntimeModeValidators;
using Umbraco.Skills.Examples.Configuration;

namespace Umbraco_CMS.Skills.TestHost;

/// <summary>
/// Proves the umbraco-configuration skill's golden profiles are real Umbraco configuration.
///
/// Three independent checks, because each catches what the others cannot:
///  - Strict binding into Umbraco's own options classes (found by their [UmbracoOptions] key, so the
///    key list is Umbraco's, not ours). The only check that knows the REAL section names — the
///    generated JSON schema is built from property names and is wrong for at least
///    DistributedJobs, which a schema-only gate would reject when written correctly.
///  - The shipped appsettings-schema.Umbraco.Cms.json, because that is what the user's editor and
///    Claude consult; the skill must agree with it wherever the schema is right.
///  - Umbraco's own runtime-mode validators, so "production" means what the host enforces at boot.
///
/// None of this boots a site: a load-balanced or Azure profile could not start in CI.
/// </summary>
[TestFixture]
public class ConfigurationGoldenTests
{
    private const string CmsPrefix = "Umbraco:CMS";

    // Sections that live under Umbraco: but belong to packages, not the CMS options/schema.
    private static readonly string[] PackageSections = ["Storage"];

    private static readonly Assembly ExampleAssembly = typeof(ConfiguredServerRoleComposer).Assembly;

    // Options classes are spread across Umbraco's assemblies (Debug lives outside Core), so scan every
    // Umbraco.* assembly in the output folder rather than just the one GlobalSettings is in.
    private static readonly Lazy<Dictionary<string, Type>> OptionsByKey = new(() =>
        LoadUmbracoAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Select(type => (Type: type, Attribute: type.GetCustomAttribute<UmbracoOptionsAttribute>()))
            .Where(pair => pair.Attribute is not null)
            .GroupBy(pair => pair.Attribute!.ConfigurationKey)
            .ToDictionary(group => group.Key, group => group.First().Type, StringComparer.OrdinalIgnoreCase));

    private static IEnumerable<Assembly> LoadUmbracoAssemblies()
    {
        foreach (string path in Directory.GetFiles(AppContext.BaseDirectory, "Umbraco.*.dll"))
        {
            Assembly? assembly = null;
            try
            {
                assembly = Assembly.LoadFrom(path);
            }
            catch (BadImageFormatException)
            {
                // native or satellite file — not an options source
            }

            if (assembly is not null)
            {
                yield return assembly;
            }
        }
    }

    // Each profile as it is deployed: file(s) layered in order, later wins.
    public static IEnumerable<TestCaseData> Profiles()
    {
        yield return new TestCaseData((object)new[] { "production.json" }).SetName("production");
        yield return new TestCaseData((object)new[] { "load-balanced.json", "load-balanced-scheduling-publisher.json" })
            .SetName("load-balanced + scheduling publisher");
        yield return new TestCaseData((object)new[] { "load-balanced.json", "load-balanced-subscriber.json" })
            .SetName("load-balanced + subscriber");
        yield return new TestCaseData((object)new[] { "azure-app-service.json" }).SetName("azure app service");
    }

    [TestCaseSource(nameof(Profiles))]
    public void Every_section_and_key_binds_into_Umbracos_own_options_classes(string[] files)
    {
        IConfiguration config = Build(files);

        IConfigurationSection cms = config.GetSection(CmsPrefix);
        Assert.That(cms.GetChildren(), Is.Not.Empty, "profile has no Umbraco:CMS section at all");

        foreach (IConfigurationSection section in cms.GetChildren())
        {
            Assert.That(
                OptionsByKey.Value.TryGetValue(section.Path, out Type? optionsType),
                $"'{section.Path}' is not an Umbraco options section. A section name Umbraco does not " +
                "bind is silently ignored at runtime — the setting would never take effect.");

            // ErrorOnUnknownConfiguration turns a mistyped or removed key into an exception.
            Assert.DoesNotThrow(
                () => section.Get(optionsType!, options => options.ErrorOnUnknownConfiguration = true),
                $"'{section.Path}' contains a key or value Umbraco's {optionsType!.Name} cannot bind");
        }
    }

    [TestCaseSource(nameof(Profiles))]
    public void Every_key_exists_in_the_shipped_json_schema(string[] files)
    {
        JsonElement schema = LoadSchema();

        foreach (string file in files)
        {
            using JsonDocument doc = JsonDocument.Parse(ReadAsset(file));
            if (!doc.RootElement.TryGetProperty("Umbraco", out JsonElement umbraco)
                || !umbraco.TryGetProperty("CMS", out JsonElement cms))
            {
                continue; // overlay with nothing under Umbraco:CMS
            }

            JsonElement cmsSchema = Resolve(schema, Property(schema, Resolve(schema, Property(schema, schema, "Umbraco")), "CMS"));
            Assert.That(SchemaViolations(schema, cmsSchema, cms, CmsPrefix), Is.Empty, $"{file} disagrees with the schema");
        }
    }

    [Test]
    public void Schema_section_names_that_do_not_match_the_keys_Umbraco_binds_are_the_known_ones()
    {
        // The schema is generated from property names; the binder uses [UmbracoOptions] keys. They
        // differ for DistributedJobs ("DistributedJobSettings" in the schema, "DistributedJobs" bound).
        // Debug: the schema says Umbraco:CMS:Debug but Umbraco 17.5.3 still binds Umbraco:CMS:Core:Debug
        // (the Umbraco source on a newer branch has moved it — always check the PINNED version).
        // InstallDefaultData is not drift so much as a different shape: it is bound as named options
        // per sub-section rather than via [UmbracoOptions]. The skill tells Claude to trust the docs
        // and binding key over the schema where they disagree. If this list changes — Umbraco fixed
        // it, or a new drift appeared — the skill's guidance needs revisiting, hence the exact assertion.
        JsonElement schema = LoadSchema();
        JsonElement cmsSchema = Resolve(schema, Property(schema, Resolve(schema, Property(schema, schema, "Umbraco")), "CMS"));

        string[] drift = cmsSchema.GetProperty("properties").EnumerateObject()
            .Select(property => property.Name)
            .Where(name => !OptionsByKey.Value.ContainsKey($"{CmsPrefix}:{name}"))
            .Order()
            .ToArray();

        Assert.That(drift, Is.EqualTo(new[] { "Debug", "DistributedJobSettings", "InstallDefaultData" }));
    }

    [TestCaseSource(nameof(Profiles))]
    public void Profiles_pass_Umbracos_production_runtime_validators(string[] files)
    {
        IConfiguration config = Build(files);

        Assert.That(config.GetValue<string>($"{CmsPrefix}:Runtime:Mode"), Is.EqualTo(nameof(RuntimeMode.Production)));
        Assert.That(config.GetValue<bool>($"{CmsPrefix}:Hosting:Debug"), Is.False);
        AssertProductionValidatorsPass(config);
    }

    [Test]
    public void The_production_validators_actually_discriminate()
    {
        // Negative control: without it, a validator stub that always returned true would make the
        // test above pass for any profile.
        IConfiguration config = Build(["production.json"], new Dictionary<string, string?>
        {
            [$"{CmsPrefix}:ModelsBuilder:ModelsMode"] = "InMemoryAuto",
            [$"{CmsPrefix}:Global:UseHttps"] = "false",
            [$"{CmsPrefix}:WebRouting:UmbracoApplicationUrl"] = string.Empty,
        });

        Assert.That(ValidatorResults(config, RuntimeMode.Production).Where(result => !result.Passed).Select(r => r.Name),
            Is.EquivalentTo(new[]
            {
                nameof(ModelsBuilderModeValidator), nameof(UseHttpsValidator), nameof(UmbracoApplicationUrlValidator),
            }));
    }

    [Test]
    public void No_profile_enables_in_memory_models()
    {
        foreach (TestCaseData profile in Profiles())
        {
            IConfiguration config = Build((string[])profile.Arguments[0]!);
            Assert.That(config.GetValue<string>($"{CmsPrefix}:ModelsBuilder:ModelsMode"), Is.EqualTo("Nothing"), profile.TestName);
        }
    }

    [Test]
    public void Load_balanced_roles_differ_only_in_examine_factory_and_role()
    {
        IConfiguration publisher = Build(["load-balanced.json", "load-balanced-scheduling-publisher.json"]);
        IConfiguration subscriber = Build(["load-balanced.json", "load-balanced-subscriber.json"]);

        Assert.Multiple(() =>
        {
            Assert.That(publisher.GetSection($"{CmsPrefix}:Examine").Get<IndexCreatorSettings>()!.LuceneDirectoryFactory,
                Is.EqualTo(LuceneDirectoryFactory.SyncedTempFileSystemDirectoryFactory));
            Assert.That(subscriber.GetSection($"{CmsPrefix}:Examine").Get<IndexCreatorSettings>()!.LuceneDirectoryFactory,
                Is.EqualTo(LuceneDirectoryFactory.TempFileSystemDirectoryFactory));

            Assert.That(ConfiguredServerRoleComposer.ParseRole(publisher[ConfiguredServerRoleComposer.ConfigKey]), Is.EqualTo(ServerRole.SchedulingPublisher));
            Assert.That(ConfiguredServerRoleComposer.ParseRole(subscriber[ConfiguredServerRoleComposer.ConfigKey]), Is.EqualTo(ServerRole.Subscriber));

            // Both roles share every other Umbraco setting, so a front end cannot drift from the publisher.
            Assert.That(Flatten(publisher, $"{CmsPrefix}").Where(pair => !pair.Key.StartsWith($"{CmsPrefix}:Examine", StringComparison.OrdinalIgnoreCase)),
                Is.EquivalentTo(Flatten(subscriber, $"{CmsPrefix}").Where(pair => !pair.Key.StartsWith($"{CmsPrefix}:Examine", StringComparison.OrdinalIgnoreCase))));
        });
    }

    [Test]
    public void Load_balanced_and_azure_profiles_set_the_multi_instance_file_settings()
    {
        foreach (string[] files in new[]
                 {
                     new[] { "load-balanced.json" },
                     ["azure-app-service.json"],
                 })
        {
            IConfiguration config = Build(files);
            Assert.Multiple(() =>
            {
                Assert.That(config.GetSection($"{CmsPrefix}:Global").Get<GlobalSettings>()!.MainDomLock, Is.EqualTo("FileSystemMainDomLock"), files[0]);
                Assert.That(config.GetSection($"{CmsPrefix}:Hosting").Get<HostingSettings>()!.LocalTempStorageLocation, Is.EqualTo(LocalTempStorage.EnvironmentTemp), files[0]);
            });
        }
    }

    [Test]
    public void Azure_profile_uses_the_synced_examine_factory_and_a_blob_container()
    {
        IConfiguration config = Build(["azure-app-service.json"]);

        Assert.That(config.GetSection($"{CmsPrefix}:Examine").Get<IndexCreatorSettings>()!.LuceneDirectoryFactory,
            Is.EqualTo(LuceneDirectoryFactory.SyncedTempFileSystemDirectoryFactory));
        Assert.That(config["Umbraco:Storage:AzureBlob:Media:ContainerName"], Is.EqualTo("mysitestorage"));
    }

    [Test]
    public void Azure_app_settings_use_valid_umbraco_keys_and_carry_no_literal_secrets()
    {
        List<KeyValuePair<string, string?>> pairs = ReadAppSettings("azure-app-service-settings.json");
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(pairs).Build();

        Assert.Multiple(() =>
        {
            Assert.That(config["ASPNETCORE_ENVIRONMENT"], Is.EqualTo("Production"));
            Assert.That(config["ConnectionStrings:umbracoDbDSN_ProviderName"], Is.EqualTo("Microsoft.Data.SqlClient"));
            Assert.That(config["ConnectionStrings:umbracoDbDSN"], Does.StartWith("@Microsoft.KeyVault(SecretUri=https://"));

            // Env-var names keep the appsettings casing (Umbraco__CMS__...); each must land on a real,
            // strictly-bindable Umbraco key.
            foreach (IConfigurationSection section in config.GetSection(CmsPrefix).GetChildren())
            {
                Assert.That(OptionsByKey.Value.TryGetValue(section.Path, out Type? type), section.Path);
                Assert.DoesNotThrow(() => section.Get(type!, options => options.ErrorOnUnknownConfiguration = true), section.Path);
            }

            Assert.That(config.GetSection($"{CmsPrefix}:WebRouting").Get<WebRoutingSettings>()!.UmbracoApplicationUrl,
                Is.EqualTo("https://www.example.com/"));
        });

        AssertNoLiteralSecrets(config, "azure-app-service-settings.json");
    }

    [Test]
    public void Azure_secrets_and_the_application_url_are_slot_settings()
    {
        // A secret or per-environment value that is NOT sticky to its slot travels with a slot swap
        // and points production at staging's database.
        using JsonDocument doc = JsonDocument.Parse(ReadAsset("azure-app-service-settings.json"));
        Dictionary<string, bool> sticky = doc.RootElement.EnumerateArray()
            .ToDictionary(item => item.GetProperty("name").GetString()!, item => item.GetProperty("slotSetting").GetBoolean());

        Assert.Multiple(() =>
        {
            Assert.That(sticky["ConnectionStrings__umbracoDbDSN"], Is.True);
            Assert.That(sticky["Umbraco__Storage__AzureBlob__Media__ConnectionString"], Is.True);
            Assert.That(sticky["Umbraco__CMS__WebRouting__UmbracoApplicationUrl"], Is.True);
        });
    }

    [TestCaseSource(nameof(Profiles))]
    public void No_profile_carries_a_literal_secret(string[] files) =>
        AssertNoLiteralSecrets(Build(files), string.Join(" + ", files));

    [TestCase(null, null)]
    [TestCase("", null)]
    [TestCase("  ", null)]
    [TestCase("SchedulingPublisher", ServerRole.SchedulingPublisher)]
    [TestCase("subscriber", ServerRole.Subscriber)]
    public void Server_role_parses_explicit_roles_and_treats_unset_as_elect(string? configured, ServerRole? expected) =>
        Assert.That(ConfiguredServerRoleComposer.ParseRole(configured), Is.EqualTo(expected));

    [TestCase("Subscribr")]
    [TestCase("Single")]
    [TestCase("Unknown")]
    [TestCase("3")]
    public void A_typo_or_non_explicit_role_fails_the_boot_instead_of_being_ignored(string configured) =>
        Assert.Throws<InvalidOperationException>(() => ConfiguredServerRoleComposer.ParseRole(configured));

    [Test]
    public void Composer_registers_the_configured_role_and_is_inert_when_unset()
    {
        var pinned = new StubBuilder(new ConfigurationBuilder()
            .AddInMemoryCollection([new(ConfiguredServerRoleComposer.ConfigKey, "Subscriber")]).Build());
        new ConfiguredServerRoleComposer().Compose(pinned);

        var unset = new StubBuilder(new ConfigurationBuilder().Build());
        new ConfiguredServerRoleComposer().Compose(unset);

        Assert.Multiple(() =>
        {
            Assert.That(pinned.Services.BuildServiceProvider().GetRequiredService<IServerRoleAccessor>().CurrentServerRole,
                Is.EqualTo(ServerRole.Subscriber));
            Assert.That(unset.Services.BuildServiceProvider().GetService<IServerRoleAccessor>(), Is.Null,
                "unset must leave Umbraco's own election in place — the composer ships in a shared host");
        });
    }

    // ---- helpers -------------------------------------------------------------------------------

    private static IConfiguration Build(string[] files, Dictionary<string, string?>? overrides = null)
    {
        var builder = new ConfigurationBuilder();
        foreach (string file in files)
        {
            builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ReadAsset(file))));
        }

        if (overrides is not null)
        {
            builder.AddInMemoryCollection(overrides);
        }

        return builder.Build();
    }

    private static string ReadAsset(string name)
    {
        using Stream? stream = ExampleAssembly.GetManifestResourceStream(name);
        Assert.That(stream, Is.Not.Null, $"'{name}' is not embedded — is it listed in .generate.json?");
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    private static List<KeyValuePair<string, string?>> ReadAppSettings(string name)
    {
        using JsonDocument doc = JsonDocument.Parse(ReadAsset(name));
        return doc.RootElement.EnumerateArray()
            .Select(item => new KeyValuePair<string, string?>(
                item.GetProperty("name").GetString()!.Replace("__", ":"),
                item.GetProperty("value").GetString()))
            .ToList();
    }

    private static Dictionary<string, string?> Flatten(IConfiguration config, string path) =>
        config.GetSection(path).AsEnumerable(makePathsRelative: false)
            .Where(pair => pair.Value is not null)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    private static void AssertNoLiteralSecrets(IConfiguration config, string label)
    {
        foreach (KeyValuePair<string, string?> pair in config.AsEnumerable().Where(pair => !string.IsNullOrEmpty(pair.Value)))
        {
            string leaf = pair.Key[(pair.Key.LastIndexOf(':') + 1)..];
            bool secretShaped =
                leaf.Contains("Password", StringComparison.OrdinalIgnoreCase)
                || leaf.Contains("Secret", StringComparison.OrdinalIgnoreCase)
                || leaf.Contains("AccountKey", StringComparison.OrdinalIgnoreCase)
                || leaf.EndsWith("ConnectionString", StringComparison.OrdinalIgnoreCase)
                || (pair.Key.StartsWith("ConnectionStrings:", StringComparison.OrdinalIgnoreCase)
                    && !leaf.EndsWith("_ProviderName", StringComparison.OrdinalIgnoreCase));

            if (secretShaped)
            {
                Assert.That(pair.Value, Does.StartWith("@Microsoft.KeyVault("),
                    $"{label}: '{pair.Key}' looks like a secret but is a literal. Use a Key Vault reference, user-secrets or an environment variable.");
            }
        }
    }

    private static void AssertProductionValidatorsPass(IConfiguration config) =>
        Assert.That(ValidatorResults(config, RuntimeMode.Production).Where(result => !result.Passed)
                .Select(result => $"{result.Name}: {result.Message}"),
            Is.Empty);

    // JITOptimizerValidator is deliberately absent: it inspects how THIS test assembly was built
    // (Debug here), not the configuration under test.
    private static List<(string Name, bool Passed, string? Message)> ValidatorResults(IConfiguration config, RuntimeMode mode)
    {
        GlobalSettings global = config.GetSection($"{CmsPrefix}:Global").Get<GlobalSettings>() ?? new GlobalSettings();
        WebRoutingSettings routing = config.GetSection($"{CmsPrefix}:WebRouting").Get<WebRoutingSettings>() ?? new WebRoutingSettings();
        ModelsBuilderSettings models = config.GetSection($"{CmsPrefix}:ModelsBuilder").Get<ModelsBuilderSettings>() ?? new ModelsBuilderSettings();

        IRuntimeModeValidator[] validators =
        [
            new UseHttpsValidator(new Monitor<GlobalSettings>(global)),
            new UmbracoApplicationUrlValidator(new Monitor<WebRoutingSettings>(routing)),
            new ModelsBuilderModeValidator(new Monitor<ModelsBuilderSettings>(models)),
        ];

        return validators
            .Select(validator => (validator.GetType().Name, validator.Validate(mode, out string? message), message))
            .ToList();
    }

    private static JsonElement LoadSchema()
    {
        // The schema is generated into the TestHost project folder by the Umbraco build and is not
        // committed. Absent means "build first", not "skip": a silent skip would pass an unchecked skill.
        string? dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "Umbraco-CMS.Skills.sln")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        Assert.That(dir, Is.Not.Null, "could not locate the repository root from the test output folder");
        string path = Path.Combine(dir!, "Umbraco-CMS.Skills.TestHost", "appsettings-schema.Umbraco.Cms.json");
        Assert.That(File.Exists(path), $"{path} is generated by the build — run `dotnet build` first");

        return JsonDocument.Parse(File.ReadAllText(path)).RootElement.Clone();
    }

    private static JsonElement Property(JsonElement schema, JsonElement node, string name)
    {
        JsonElement resolved = Resolve(schema, node);
        Assert.That(resolved.TryGetProperty("properties", out JsonElement properties) && properties.TryGetProperty(name, out JsonElement property),
            $"schema has no '{name}'");
        return resolved.GetProperty("properties").GetProperty(name);
    }

    // $ref / oneOf / allOf wrappers: the generated schema nests every section as { "oneOf": [ { "$ref": ... } ] }
    // or a bare $ref. Follow to the first concrete (non-null) target.
    private static JsonElement Resolve(JsonElement schema, JsonElement node)
    {
        while (true)
        {
            if (node.TryGetProperty("$ref", out JsonElement reference))
            {
                string name = reference.GetString()!.Split('/').Last();
                node = schema.GetProperty("definitions").GetProperty(name);
                continue;
            }

            foreach (string combinator in new[] { "oneOf", "allOf", "anyOf" })
            {
                if (node.TryGetProperty(combinator, out JsonElement options))
                {
                    JsonElement? next = options.EnumerateArray()
                        .Where(option => !(option.TryGetProperty("type", out JsonElement type) && type.ValueKind == JsonValueKind.String && type.GetString() == "null"))
                        .Select(option => (JsonElement?)option)
                        .FirstOrDefault();
                    if (next is not null)
                    {
                        node = next.Value;
                        goto resolved;
                    }
                }
            }

            return node;
            resolved:;
        }
    }

    private static IEnumerable<string> SchemaViolations(JsonElement schema, JsonElement node, JsonElement value, string path)
    {
        node = Resolve(schema, node);

        if (value.ValueKind == JsonValueKind.Object && node.TryGetProperty("properties", out JsonElement properties))
        {
            foreach (JsonProperty child in value.EnumerateObject())
            {
                if (!properties.TryGetProperty(child.Name, out JsonElement childSchema))
                {
                    yield return $"{path}:{child.Name} is not in the schema";
                    continue;
                }

                foreach (string violation in SchemaViolations(schema, childSchema, child.Value, $"{path}:{child.Name}"))
                {
                    yield return violation;
                }
            }
        }
        else if (value.ValueKind == JsonValueKind.String && node.TryGetProperty("enum", out JsonElement allowed)
                 && !allowed.EnumerateArray().Any(option => string.Equals(option.GetString(), value.GetString(), StringComparison.OrdinalIgnoreCase)))
        {
            yield return $"{path} = '{value.GetString()}' is not one of: {string.Join(", ", allowed.EnumerateArray().Select(option => option.GetString()))}";
        }
    }

    private sealed class Monitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class StubBuilder(IConfiguration config) : IUmbracoBuilder
    {
        public IServiceCollection Services { get; } = new ServiceCollection();

        public IConfiguration Config => config;

        public TypeLoader TypeLoader => null!;

        public ILoggerFactory BuilderLoggerFactory => null!;

        public IProfiler Profiler => null!;

        public AppCaches AppCaches => null!;

        public TBuilder WithCollectionBuilder<TBuilder>()
            where TBuilder : ICollectionBuilder => throw new NotSupportedException();

        public void Build()
        {
        }
    }
}
