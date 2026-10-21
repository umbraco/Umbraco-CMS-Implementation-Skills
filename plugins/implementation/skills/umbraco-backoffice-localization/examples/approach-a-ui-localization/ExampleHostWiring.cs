using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Skills.Examples.BackofficeLocalization.UiLocalization;

/// <summary>
/// This example's own harness, not part of the skill.
///
/// A real user copies the skill's files into App_Plugins by hand. Here the host project is shared, so
/// the same files are written from the embedded copies at start-up, before Umbraco reads App_Plugins.
/// The folders are git-ignored in the host project. This runs on every boot of the Blank host, so its
/// backoffice then says "Domains" for "Culture and Hostnames"; the umbraco-reference-instance skill
/// lists these files and how to remove them.
///
/// Also writes the skill's manifest into a NESTED folder, which the docs say is not registered. The
/// test uses it as a control: it proves a manifest in the right place registers while an identical
/// one a level deeper does not, so the "directly in App_Plugins or one folder down" rule is observed
/// rather than assumed.
/// </summary>
public class LocalizationExampleComposer : IComposer
{
    public const string PackageName = "Skills.Localization";
    public const string NestedPackageName = "Skills.Localization.Nested";

    public void Compose(IUmbracoBuilder builder)
    {
        // The host's content root is in configuration under "contentRoot"; the builder has no hosting environment.
        var contentRoot = builder.Config["contentRoot"]
            ?? throw new InvalidOperationException("The host configuration has no contentRoot.");
        var plugins = Path.Combine(contentRoot, "App_Plugins");

        Write(Path.Combine(plugins, PackageName, "umbraco-package.json"), Resource("umbraco-package.json"));
        Write(Path.Combine(plugins, PackageName, "en.js"), Resource("en.js"));
        Write(Path.Combine(plugins, PackageName, "da.js"), Resource("da.js"));

        // The overrides ship as their own package in their own folder.
        Write(
            Path.Combine(plugins, PackageName + ".Overrides", "umbraco-package.json"),
            Resource("core-overrides-package.json"));

        Write(
            Path.Combine(plugins, NestedPackageName, "Localization", "umbraco-package.json"),
            Resource("umbraco-package.json").Replace(
                $"\"name\": \"{PackageName}\"", $"\"name\": \"{NestedPackageName}\"", StringComparison.Ordinal));
    }

    private static string Resource(string name)
    {
        using Stream stream = typeof(LocalizationExampleComposer).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded asset '{name}' is missing from the example.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path) || File.ReadAllText(path) != content)
        {
            File.WriteAllText(path, content);
        }
    }
}
