using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Acme.Web.Startup;

/// <summary>
/// Startup job for the Danish launch. After the package migration has added da-DK, this writes the
/// first Danish values onto our existing, already-published English pages and publishes the whole
/// tree so the Danish pages go live. Idempotent so it can run on every environment at boot.
///
/// Problem: after deploying, the Danish edits are saved (they show in the backoffice) but never go
/// live on the site.
/// </summary>
public class DanishContentSeeder : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private const string Danish = "da-DK";

    private readonly IContentService _contentService;
    private readonly IContentPublishingService _contentPublishingService;
    private readonly ILogger<DanishContentSeeder> _logger;

    public DanishContentSeeder(
        IContentService contentService,
        IContentPublishingService contentPublishingService,
        ILogger<DanishContentSeeder> logger)
    {
        _contentService = contentService;
        _contentPublishingService = contentPublishingService;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        IContent? root = _contentService.GetRootContent().FirstOrDefault();
        if (root is null)
        {
            _logger.LogWarning("No root content found; Danish seeding skipped.");
            return;
        }

        // Launch copy for the pages that must have Danish on day one. Editors translate the rest.
        SetDanish(root, "Forside", ("title", "Velkommen til Acme"));

        foreach (IContent page in _contentService.GetPagedChildren(root.Id, 0, 100, out _, propertyAliases: null, filter: null, ordering: null))
        {
            switch (page.ContentType.Alias)
            {
                case "aboutPage":
                    SetDanish(page, "Om os", ("title", "Om Acme"));
                    break;
                case "contactPage":
                    SetDanish(page, "Kontakt", ("title", "Kontakt os"));
                    break;
            }
        }

        // Publish everything under the root in every culture so the Danish pages go live.
        var result = await _contentPublishingService.PublishBranchAsync(
            root.Key,
            ["*"],
            PublishBranchFilter.IncludeUnpublished,
            Constants.Security.SuperUserKey,
            false);

        if (!result.Success)
        {
            _logger.LogError("Publishing the Danish launch branch failed with {Status}.", result.Status);
        }
    }

    private void SetDanish(IContent node, string danishName, params (string Alias, string Value)[] values)
    {
        node.SetCultureName(danishName, Danish);
        foreach ((string alias, string value) in values)
        {
            node.SetValue(alias, value, Danish);
        }

        _contentService.Save(node);
    }
}

public class DanishContentSeederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, DanishContentSeeder>();
}
