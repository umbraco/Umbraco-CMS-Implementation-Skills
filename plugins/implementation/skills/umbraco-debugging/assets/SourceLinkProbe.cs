using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace <Namespace>;

// A throwaway F11 target for stepping into Umbraco's source with Source Link.
// On startup it calls one Umbraco service method, so you can set a breakpoint on that line,
// launch under the debugger (F5) and step into Umbraco without clicking anything in the site.
// Delete this file when you are done: it runs a database query on every boot.
//
// Uses a notification handler rather than IComponent, which is obsolete in Umbraco 17.

public class SourceLinkProbeComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<SourceLinkProbeState>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, SourceLinkProbeHandler>();
    }
}

// Records what the probe saw, so you (or a test) can confirm it ran.
public class SourceLinkProbeState
{
    public bool Ran { get; set; }

    public int RootCount { get; set; }
}

public class SourceLinkProbeHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IContentService _contentService;
    private readonly IRuntimeState _runtimeState;
    private readonly SourceLinkProbeState _state;

    public SourceLinkProbeHandler(
        IContentService contentService,
        IRuntimeState runtimeState,
        SourceLinkProbeState state)
    {
        _contentService = contentService;
        _runtimeState = runtimeState;
        _state = state;
    }

    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        // Content services aren't usable until Umbraco is installed and running.
        if (_runtimeState.Level != RuntimeLevel.Run)
        {
            return Task.CompletedTask;
        }

        // Set a breakpoint on the next line and press F11 to step into ContentService.GetRootContent
        // in Umbraco's source. If F11 steps over it, check Just My Code, Source Link and the
        // NuGet.org symbol server settings.
        IEnumerable<IContent> roots = _contentService.GetRootContent();

        _state.RootCount = roots.Count();
        _state.Ran = true;

        return Task.CompletedTask;
    }
}
