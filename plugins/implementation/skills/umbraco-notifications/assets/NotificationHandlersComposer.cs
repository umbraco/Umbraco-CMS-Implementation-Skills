using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace <Namespace>;

public class NotificationHandlersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationHandler<ContentPublishingNotification, PublishingRuleHandler>();
        // Register further handlers here, e.g.
        // builder.AddNotificationAsyncHandler<ContentPublishedNotification, MyAsyncHandler>();
    }
}
