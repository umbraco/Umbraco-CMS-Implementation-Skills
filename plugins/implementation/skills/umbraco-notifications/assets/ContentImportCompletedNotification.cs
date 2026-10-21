using Umbraco.Cms.Core.Notifications;

namespace <Namespace>;

/// <summary>
/// Raised once a content import has committed. Publish it via scope.Notifications so it is
/// dropped together with the work if the scope is disposed without Complete().
/// </summary>
public class ContentImportCompletedNotification : INotification
{
    public ContentImportCompletedNotification(string source, int itemsImported)
    {
        Source = source;
        ItemsImported = itemsImported;
    }

    public string Source { get; }
    public int ItemsImported { get; }
}
