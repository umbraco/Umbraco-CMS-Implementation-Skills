using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;

namespace <Namespace>;

public class ContentImportCompletedHandler : INotificationHandler<ContentImportCompletedNotification>
{
    private readonly ILogger<ContentImportCompletedHandler> _logger;

    public ContentImportCompletedHandler(ILogger<ContentImportCompletedHandler> logger) => _logger = logger;

    // Replace the body with the real reaction. Handlers are transient: keep state in an injected service.
    public void Handle(ContentImportCompletedNotification notification) =>
        _logger.LogInformation("Import from {Source} completed with {Count} items", notification.Source, notification.ItemsImported);
}
