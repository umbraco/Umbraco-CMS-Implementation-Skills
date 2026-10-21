using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Skills.Examples.NotificationHandlersApproachB;

namespace Umbraco_CMS.Skills.TestHost;

/// <summary>
/// Deterministic runtime validation of the umbraco-notifications skill (Approach B).
/// Proves the shipped ContentImportCompletedNotification is delivered to handlers registered
/// the way the reference instructs, both when published immediately via IEventAggregator and
/// when queued on a scope, and that an uncompleted scope drops it.
///
/// Each test publishes with a unique Source so they cannot observe each other's notifications.
/// </summary>
[TestFixture]
public class NotificationPublishingTests
{
    private static T Resolve<T>() where T : notnull =>
        ReferenceSiteFixture.Factory.Services.GetRequiredService<T>();

    [Test]
    public void Publish_via_IEventAggregator_delivers_immediately()
    {
        string source = $"aggregator-{Guid.NewGuid():N}";

        Resolve<IEventAggregator>().Publish(new ContentImportCompletedNotification(source, 3));

        Assert.That(
            Resolve<ImportNotificationRecorder>().Received,
            Has.Some.Matches<ContentImportCompletedNotification>(
                n => n.Source == source && n.ItemsImported == 3),
            "a handler registered with AddNotificationHandler must receive a notification "
            + "published through IEventAggregator");
    }

    [Test]
    public void Publish_on_a_completed_scope_delivers_on_completion()
    {
        string source = $"scope-completed-{Guid.NewGuid():N}";

        using (ICoreScope scope = Resolve<ICoreScopeProvider>().CreateCoreScope())
        {
            scope.Notifications.Publish(new ContentImportCompletedNotification(source, 7));
            scope.Complete();
        }

        Assert.That(
            Resolve<ImportNotificationRecorder>().Received,
            Has.Some.Matches<ContentImportCompletedNotification>(
                n => n.Source == source && n.ItemsImported == 7),
            "scope.Notifications.Publish must dispatch queued notifications when the scope "
            + "completes and is disposed");
    }

    [Test]
    public void Publish_on_an_uncompleted_scope_is_dropped()
    {
        string source = $"scope-dropped-{Guid.NewGuid():N}";

        using (ICoreScope scope = Resolve<ICoreScopeProvider>().CreateCoreScope())
        {
            scope.Notifications.Publish(new ContentImportCompletedNotification(source, 5));
            // No Complete(): disposing the scope rolls the queued notification back with the work.
        }

        Assert.That(
            Resolve<ImportNotificationRecorder>().Received,
            Has.None.Matches<ContentImportCompletedNotification>(n => n.Source == source),
            "a notification published into a scope that never completed must be silently dropped");
    }
}
