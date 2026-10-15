using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Acme.Site.Infrastructure;

// Some deployments switch this off with [assembly: DisableComposer(typeof(AnalyticsComposer))].
public class AnalyticsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Registers the analytics tracker and its content finder (omitted).
    }
}
