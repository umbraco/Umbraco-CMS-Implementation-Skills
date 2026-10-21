using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Manifest;

namespace Umbraco_CMS.Skills.TestHost.Blank;

/// <summary>
/// Deterministic runtime validation of umbraco-backoffice-localization approach A: the skill's
/// manifest, JS modules and core-override manifest (en and da), written under App_Plugins by the example's
/// ExampleHostWiring.cs and read back through Umbraco's own package manifest service and static files.
///
/// Runs on SITE 2. It proves Umbraco REGISTERS the localization extensions and SERVES the modules; the
/// backoffice client that merges them (weight, fallback) runs in a browser and is not exercised here.
/// </summary>
[TestFixture]
public class BackofficeLocalizationBlankTests
{
    private const string PackageName = "Skills.Localization";

    private static IServiceProvider Services => BlankSiteFixture.Factory.Services;

    private static async Task<IReadOnlyList<PackageManifest>> ManifestsAsync() =>
        (await Services.GetRequiredService<IPackageManifestService>().GetAllPackageManifestsAsync()).ToList();

    /// <summary>Each extension of the package as parsed JSON, so the test reads the manifest as written.</summary>
    private static List<JsonElement> Extensions(PackageManifest manifest) =>
        manifest.Extensions
            .Select(e => JsonSerializer.SerializeToElement(e))
            .ToList();

    [Test]
    public async Task Manifest_in_App_Plugins_registers_an_extension_per_culture()
    {
        PackageManifest manifest = (await ManifestsAsync()).SingleOrDefault(m => m.Name == PackageName)
            ?? throw new AssertionException($"package '{PackageName}' was not registered");

        List<JsonElement> extensions = Extensions(manifest);
        Assert.That(extensions.Select(e => e.GetProperty("type").GetString()), Has.All.EqualTo("localization"));

        string?[] cultures = extensions.Select(e => e.GetProperty("meta").GetProperty("culture").GetString()).ToArray();
        Assert.That(cultures, Is.EquivalentTo(new[] { "en", "da" }));

        string?[] aliases = extensions.Select(e => e.GetProperty("alias").GetString()).ToArray();
        Assert.That(aliases, Is.Unique, "every localization extension needs its own alias");
    }

    [Test]
    public async Task Manifest_in_a_nested_folder_is_not_registered()
    {
        Assert.That((await ManifestsAsync()).Select(m => m.Name), Does.Not.Contain("Skills.Localization.Nested"),
            "the identical manifest one folder deeper must be ignored, which is why the skill puts it directly under App_Plugins/<Package>/");
    }

    [TestCase("en", "Domains")]
    [TestCase("da", "Domæner")]
    public async Task Override_manifest_carries_a_weight_below_the_default_and_the_core_key(string culture, string expectedText)
    {
        PackageManifest manifest = (await ManifestsAsync()).SingleOrDefault(m => m.Name == PackageName + ".Overrides")
            ?? throw new AssertionException("the overrides package was not registered");

        JsonElement extension = Extensions(manifest)
            .SingleOrDefault(e => e.GetProperty("meta").GetProperty("culture").GetString() == culture);
        Assert.That(extension.ValueKind, Is.EqualTo(JsonValueKind.Object),
            $"the overrides package needs a '{culture}' extension: a core translation in the editor's language beats an 'en' override");
        Assert.That(extension.GetProperty("weight").GetInt32(), Is.LessThan(100),
            "Umbraco's own translations use the default weight 100, so an override must be lower");
        Assert.That(
            extension.GetProperty("meta").GetProperty("localizations").GetProperty("actions").GetProperty("assigndomain").GetString(),
            Is.EqualTo(expectedText));
    }

    [Test]
    public async Task Core_danish_dictionary_translates_the_overridden_key_itself()
    {
        // Why the overrides need a 'da' extension: Umbraco's Danish UI dictionary has its own text for
        // the key, and the backoffice reads the user's language before it falls back to 'en'.
        HttpResponseMessage response = await BlankSiteFixture.Client.GetAsync("/umbraco/backoffice/assets/lang/da.js");
        string core = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "the shipped Danish UI dictionary must be served");
        Assert.That(core, Does.Match("""assigndomain:\s*['"]Tilføj domæne['"]"""),
            "core da.js must translate actions.assigndomain itself, which is what hides an 'en'-only override from Danish editors");
    }

    [Test]
    public async Task Override_key_is_a_real_core_ui_key_with_the_same_case()
    {
        // The skill's rule that the key must be copied exactly: prove the key it overrides exists in
        // the English dictionary Umbraco ships, spelled the same way.
        HttpResponseMessage response = await BlankSiteFixture.Client.GetAsync("/umbraco/backoffice/assets/lang/en.js");
        string core = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "the shipped English UI dictionary must be served");
        Assert.That(core, Does.Match("""assigndomain:\s*['"]Culture and Hostnames['"]"""),
            "actions.assigndomain must be the core key for 'Culture and Hostnames'");
        Assert.That(core, Does.Not.Match("""assignDomain:\s*['"]Culture and Hostnames['"]"""));
    }

    [TestCase("en.js", "Welcome", "export default")]
    [TestCase("da.js", "Velkommen", "export default")]
    public async Task Localization_module_is_served_as_a_default_export(string file, string expectedText, string marker)
    {
        HttpResponseMessage response = await BlankSiteFixture.Client.GetAsync($"/App_Plugins/{PackageName}/{file}");
        string body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"{file} must be served from App_Plugins");
        Assert.That(body, Does.Contain(marker), "the backoffice imports the module and reads its default export");
        Assert.That(body, Does.Contain(expectedText));
    }
}
