using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Acme.Web.Services;

public class ArticleStatsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        builder.Services.AddSingleton<IArticleStatsClient, ArticleStatsClient>();
}
