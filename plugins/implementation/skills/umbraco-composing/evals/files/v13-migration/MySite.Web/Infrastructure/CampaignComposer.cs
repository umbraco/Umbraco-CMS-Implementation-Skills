using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Routing;

namespace Acme.Site.Infrastructure;

public class CampaignComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IRedirectCache, RedirectCache>();
        builder.ContentFinders().InsertBefore<ContentFinderByUrl, CampaignContentFinder>();
    }
}
