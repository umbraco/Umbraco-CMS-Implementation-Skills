using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Routing;

namespace Umbraco.Skills.Examples.ComposingProgramCs;

// COMPILE-ONLY stand-ins for the user's own types, NOT part of what the skill ships. The
// FeatureBuilderExtensions template registers "your notification handler" and "your content
// finder"; nothing runs them here, so they do nothing.

public sealed class ComposingExamplePublishedHandler : INotificationAsyncHandler<ContentPublishedNotification>
{
    public Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public sealed class ComposingExampleContentFinder : IContentFinder
{
    public Task<bool> TryFindContent(IPublishedRequestBuilder request) => Task.FromResult(false);
}
