using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace <Namespace>.Routing;

/// <summary>Registers the custom URL segment provider and content finder.</summary>
public class RoutingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Insert() puts it first, ahead of Umbraco's default provider. A provider returning null
        // passes the page on to the next one.
        builder.UrlSegmentProviders().Insert<SkuUrlSegmentProvider>();

        // Append() runs it after the built-in finders. Only InsertBefore<ContentFinderByUrlNew, T>()
        // when a URL that matches a real page must be answered by your finder instead: it then runs
        // on every page request.
        builder.ContentFinders().Append<SkuShortLinkContentFinder>();
    }
}
