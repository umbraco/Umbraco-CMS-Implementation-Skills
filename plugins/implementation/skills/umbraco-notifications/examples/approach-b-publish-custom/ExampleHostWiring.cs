using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;

namespace Umbraco.Skills.Examples.NotificationHandlersApproachB;

/// <summary>
/// Host wiring for the Approach B example: registers the skill's shipped handler exactly as the
/// reference instructs, plus a singleton recorder and a second, test-only handler so the fixture
/// can observe deliveries; the shipped handler only logs, which a test can't assert on.
///
/// The recorder and RecordingImportHandler live in the same assembly as the composer so Umbraco
/// discovers everything from this one project.
/// </summary>
public class ImportNotificationComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<ImportNotificationRecorder>();

        builder.AddNotificationHandler<ContentImportCompletedNotification, ContentImportCompletedHandler>();
        builder.AddNotificationHandler<ContentImportCompletedNotification, RecordingImportHandler>();
    }
}

/// <summary>Collects every ContentImportCompletedNotification a handler observes. Singleton.</summary>
public class ImportNotificationRecorder
{
    public ConcurrentBag<ContentImportCompletedNotification> Received { get; } = new();
}

/// <summary>Test-only observer that appends each notification to the shared recorder.</summary>
public class RecordingImportHandler : INotificationHandler<ContentImportCompletedNotification>
{
    private readonly ImportNotificationRecorder _recorder;

    public RecordingImportHandler(ImportNotificationRecorder recorder) => _recorder = recorder;

    public void Handle(ContentImportCompletedNotification notification) =>
        _recorder.Received.Add(notification);
}
