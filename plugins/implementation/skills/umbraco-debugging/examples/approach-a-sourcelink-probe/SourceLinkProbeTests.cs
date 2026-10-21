using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Skills.Examples.Debugging.SourceLinkProbe;

namespace Umbraco_CMS.Skills.TestHost;

/// <summary>
/// Deterministic runtime validation of umbraco-debugging's Approach A probe
/// (assets/SourceLinkProbe.cs), compiled from the skill's asset and loaded into the Clean host by its
/// composer scan.
///
/// The probe exists to be an F11 target that runs on its own at startup, so what the skill promises
/// is: the handler is registered, it fires once Umbraco is running, and the IContentService call on
/// the breakpoint line actually executes against real content. All three show up in
/// SourceLinkProbeState.
///
/// Timing: ReferenceSiteFixture's first CreateClient() boots the host, and the unattended install
/// (Clean's content included) runs inside CoreRuntime.StartAsync, before ApplicationStarted fires
/// UmbracoApplicationStartedNotification. So on a fresh database the probe should already see
/// RuntimeLevel.Run and installed content. If Ran is false here, the probe's RuntimeLevel guard
/// skipped it on the very boot a user would be debugging.
/// </summary>
[TestFixture]
public class SourceLinkProbeTests
{
    private static IServiceProvider Services => ReferenceSiteFixture.Factory.Services;

    [Test]
    public void Probe_ran_on_startup()
    {
        SourceLinkProbeState state = Services.GetRequiredService<SourceLinkProbeState>();

        Assert.That(state.Ran, Is.True,
            "SourceLinkProbeHandler should have handled UmbracoApplicationStartedNotification with the "
            + "runtime at RuntimeLevel.Run and recorded it in SourceLinkProbeState");
    }

    [Test]
    public void Probe_counted_the_installed_root_content()
    {
        SourceLinkProbeState state = Services.GetRequiredService<SourceLinkProbeState>();
        int expected = Services.GetRequiredService<IContentService>().GetRootContent().Count();

        Assert.That(expected, Is.GreaterThan(0), "precondition: Clean's install should create root content");
        Assert.That(state.RootCount, Is.EqualTo(expected),
            "the probe's GetRootContent() call should have seen the installed root nodes");
    }
}
