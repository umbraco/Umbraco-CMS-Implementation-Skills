using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace Umbraco.Skills.Examples.Composing;

// VALIDATION-ONLY stand-ins for the user's own types — NOT generated from the skill's assets/ and
// NOT part of what the skill ships. The skill's FeatureBuilderExtensions template registers "your
// notification handler" and "your content finder"; these are the smallest real types that let the
// test host prove those registrations took effect.

/// <summary>No-op handler: the tests only assert it is registered, and registered once.</summary>
public sealed class ComposingExamplePublishedHandler : INotificationAsyncHandler<ContentPublishedNotification>
{
    public Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

/// <summary>
/// Serves the site's first root node at <see cref="ProbePath"/>, a URL no core finder matches.
/// A 200 there proves the finder is live in the request pipeline; its position in the collection
/// (before ContentFinderByUrlNew) is asserted separately.
/// </summary>
public sealed class ComposingExampleContentFinder : IContentFinder
{
    public const string ProbePath = "/composing-example-probe";

    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IDocumentNavigationQueryService _documentNavigationQueryService;

    public ComposingExampleContentFinder(
        IUmbracoContextAccessor umbracoContextAccessor,
        IDocumentNavigationQueryService documentNavigationQueryService)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
        _documentNavigationQueryService = documentNavigationQueryService;
    }

    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        if (!string.Equals(request.Uri.AbsolutePath.TrimEnd('/'), ProbePath, StringComparison.OrdinalIgnoreCase)
            || !_umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext? umbracoContext)
            || !_documentNavigationQueryService.TryGetRootKeys(out IEnumerable<Guid> rootKeys))
        {
            return Task.FromResult(false);
        }

        IPublishedContent? root = rootKeys
            .Select(key => umbracoContext.Content?.GetById(key))
            .FirstOrDefault(content => content is not null);

        if (root is null)
        {
            return Task.FromResult(false);
        }

        request.SetPublishedContent(root);
        return Task.FromResult(true);
    }
}
