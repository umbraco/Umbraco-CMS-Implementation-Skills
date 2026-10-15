using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using <Namespace>.Services;

namespace <Namespace>;

// Lifetime follows dependencies: SiteService (scoped dep) → Scoped; RootContentReader and
// UnpublishedUrlStore (singletons / app state) → Singleton.
public class SiteServiceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<ISiteService, SiteService>();
        builder.Services.AddSingleton<RootContentReader>();
        builder.Services.AddSingleton<UnpublishedUrlStore>();

        builder.AddNotificationHandler<ContentUnpublishingNotification, UnpublishingUrlRecorder>();
    }
}
