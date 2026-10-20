using Umbraco_CMS.Skills.TestHost.Shared;

namespace Umbraco_CMS.Skills.TestHost.Blank;

/// <summary>
/// Boots site 2 ONCE for the whole test assembly and shares it with every Approach-B fixture.
///
/// Same constraint as site 1's fixture: one Umbraco host per process. This assembly exists as a
/// SEPARATE test project precisely so it gets a separate process from the Clean host — see
/// UmbracoHostSentinel for what goes wrong otherwise, and why CI runs the two projects as separate
/// `dotnet test` invocations rather than trusting the runner to isolate them.
/// </summary>
[SetUpFixture]
public class BlankSiteFixture
{
    /// <summary>Identifies this host to the process-wide sentinel.</summary>
    public const string HostName = "Umbraco-CMS.Skills.Blank (no starter kit)";

    /// <summary>The shared instance. Use it to create extra clients (e.g. non-redirecting).</summary>
    public static BlankSiteFactory Factory { get; private set; } = null!;

    /// <summary>Default client: site installed and package migrations applied, redirects followed.</summary>
    public static HttpClient Client { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task BootBlankSite()
    {
        UmbracoHostSentinel.Claim(HostName);

        Factory = new BlankSiteFactory();
        Client = Factory.CreateClient();

        // Wait for the seeded children of the root to be QUERYABLE, not merely for the API to answer.
        //
        // This site's content is created during startup rather than by the install, and the Delivery
        // API's query endpoint reads the Examine index, which is populated asynchronously. So the
        // endpoint starts answering 200 before the seeded content is searchable, and anything reading
        // that query — the content preconditions — sees an empty site and fails. Front-end routing goes
        // through the published cache instead and is already consistent, which is exactly why the
        // symptom looked so selective: every rendering test passed while the preconditions failed.
        //
        // "Any child is queryable" is not enough: several examples seed and publish on the same startup
        // signal, and the index can answer with some of their children before others. So wait for every
        // Document Type alias the manifests DECLARE at root — exactly what the preconditions then assert.
        //
        // That second wait is BEST-EFFORT. A declared alias that never appears (a typo, an unresolved
        // <Placeholder>, a node one level too deep) must not stall the boot and fail every test in the
        // assembly: after a bounded wait we carry on, and the precondition tests then fail for that one
        // skill, naming it and listing the root children that do exist.
        await Factory.WaitUntilInstalledAsync(
            Client,
            root => ContentPreconditions.ParseRootChildren(root).Count > 0,
            url: ContentPreconditions.RootChildrenQuery);

        string[] declaredAliases = ContentPreconditions.DeclaredRequirements(ContentPreconditions.BlankHost)
            .Where(requirement => requirement.Kind == "documentTypeAliasAtRoot")
            .Select(requirement => requirement.Value)
            .Where(alias => !alias.StartsWith('<')) // unresolved; its precondition test reports it
            .ToArray();
        string[] missing = declaredAliases;

        try
        {
            await Factory.WaitUntilInstalledAsync(
                Client,
                root =>
                {
                    var indexed = ContentPreconditions.ParseRootChildren(root)
                        .Select(child => child.ContentType)
                        .ToHashSet();
                    missing = declaredAliases.Where(alias => !indexed.Contains(alias)).ToArray();
                    return missing.Length == 0;
                },
                timeout: TimeSpan.FromSeconds(60),
                url: ContentPreconditions.RootChildrenQuery);
        }
        catch (TimeoutException)
        {
            TestContext.Progress.WriteLine(
                $"{HostName}: declared root Document Types never became queryable: "
                + $"{string.Join(", ", missing)}. Continuing; the content precondition tests report "
                + "which skill declared them.");
        }
    }

    [OneTimeTearDown]
    public void ShutDownBlankSite()
    {
        Client?.Dispose();
        Factory?.Dispose();
    }
}
