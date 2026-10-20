using System.Text.Json;

namespace Umbraco_CMS.Skills.TestHost.Shared;

/// <summary>
/// Discovers the content preconditions each skill example DECLARES, so adding a skill to the gate
/// needs no new test code and nothing here mentions any particular skill.
///
/// Requirements are read from each <c>&lt;skill&gt;/examples/&lt;approach&gt;/.generate.json</c>
/// `requires` block and filtered by that manifest's `host`, because a requirement only means anything
/// against the host that actually loads the example. Checking site 2's requirements against site 1
/// fails for a completely uninteresting reason.
/// </summary>
public static class ContentPreconditions
{
    /// <summary>Site 1 — the Clean starter kit host. The default when a manifest says nothing.</summary>
    public const string CleanHost = "clean";

    /// <summary>Site 2 — the host with no starter kit, for content/config-shaped approaches.</summary>
    public const string BlankHost = "blank";

    public static readonly string[] KnownHosts = [CleanHost, BlankHost];

    /// <summary>
    /// The Delivery API query for the site root's direct children. Shared by the preconditions and by
    /// any host fixture that waits for them, so the wait and the assertion always read the same thing.
    /// </summary>
    public const string RootChildrenQuery = "/umbraco/delivery/api/v2/content?fetch=children:/&take=100";

    /// <summary>Parses a <see cref="RootChildrenQuery"/> response into (name, contentTypeAlias) pairs.</summary>
    public static List<(string Name, string ContentType)> ParseRootChildren(JsonElement root) =>
        root.GetProperty("items").EnumerateArray()
            .Select(item => (
                Name: item.GetProperty("name").GetString() ?? string.Empty,
                ContentType: item.GetProperty("contentType").GetString() ?? string.Empty))
            .ToList();

    /// <summary>A content precondition one skill's example declared.</summary>
    public record ContentRequirement(string Skill, string Kind, string Value)
    {
        public override string ToString() => $"{Skill} needs {Kind} '{Value}'";
    }

    /// <summary>
    /// Walks up from the test assembly to the repo root — the tests run from bin/ and the manifests
    /// live next to the skills, so neither path can be hardcoded relative to the other.
    /// </summary>
    public static DirectoryInfo RepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Umbraco-CMS.Skills.sln")))
        {
            dir = dir.Parent;
        }

        return dir
            ?? throw new InvalidOperationException(
                "Could not locate the repo root (no Umbraco-CMS.Skills.sln above the test assembly).");
    }

    /// <summary>
    /// Every <c>&lt;skill&gt;/examples/&lt;approach&gt;/.generate.json</c>. Build output is skipped: the
    /// SDK copies the manifest into bin/, and those copies would look like extra examples declaring the
    /// same requirements.
    /// </summary>
    public static List<FileInfo> ExampleManifests() =>
        new DirectoryInfo(Path.Combine(RepoRoot().FullName, "plugins"))
            .GetFiles(".generate.json", SearchOption.AllDirectories)
            .Where(f => f.Directory?.Parent?.Name == "examples")
            .OrderBy(f => f.FullName)
            .ToList();

    /// <summary>Which host a manifest's example targets. Absent means site 1, the original host.</summary>
    public static string HostOf(JsonDocument manifest) =>
        manifest.RootElement.TryGetProperty("host", out JsonElement host)
            ? host.GetString() ?? CleanHost
            : CleanHost;

    /// <summary>"&lt;skill&gt;/&lt;approach&gt;", used so a failure names the example that declared it.</summary>
    private static string SkillName(FileInfo manifest) =>
        manifest.Directory?.Parent?.Parent?.Name is string s
            ? $"{s}/{manifest.Directory!.Name}"
            : manifest.FullName;

    /// <summary>Every manifest's declared host, for the guard against a typo'd value.</summary>
    public static IEnumerable<(string Skill, string Host)> DeclaredHosts()
    {
        foreach (FileInfo manifest in ExampleManifests())
        {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(manifest.FullName));
            yield return (SkillName(manifest), HostOf(doc));
        }
    }

    /// <summary>
    /// Requirements declared for one host. NUnit turns each into its own test case, so a broken
    /// precondition is reported against the skill that declared it.
    /// </summary>
    public static IEnumerable<ContentRequirement> DeclaredRequirements(string host)
    {
        foreach (FileInfo manifest in ExampleManifests())
        {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(manifest.FullName));
            if (HostOf(doc) != host)
            {
                continue;
            }

            string skill = SkillName(manifest);

            if (!doc.RootElement.TryGetProperty("requires", out JsonElement requires))
            {
                continue; // declaring nothing is fine — plenty of examples need no particular node
            }

            Dictionary<string, string> placeholders =
                doc.RootElement.TryGetProperty("placeholders", out JsonElement p)
                    ? p.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.GetString() ?? string.Empty)
                    : [];

            foreach (JsonProperty kind in requires.EnumerateObject())
            {
                foreach (JsonElement entry in kind.Value.EnumerateArray())
                {
                    string declared = entry.GetString() ?? string.Empty;

                    // "<ErrorPageAlias>" means "whatever that placeholder resolves to", so the literal
                    // value is never written twice. An unresolvable token is a manifest bug, and
                    // silently testing the token itself would hide it — hence the assertion in the base
                    // fixture rather than a quiet fallback here.
                    string value = placeholders.TryGetValue(declared, out string? resolved)
                        ? resolved
                        : declared;

                    yield return new ContentRequirement(skill, kind.Name, value);
                }
            }
        }
    }
}
