using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace <Namespace>;

/// <summary>
/// Cancels a publish when a property on one Document Type breaks a rule. The editor sees the
/// EventMessage in the backoffice and the publish does not happen; their saved draft is kept.
/// </summary>
public class PublishingRuleHandler : INotificationHandler<ContentPublishingNotification>
{
    private const string DocTypeAlias = "<DocTypeAlias>";
    private const string PropertyAlias = "<PropertyAlias>";

    public void Handle(ContentPublishingNotification notification)
    {
        foreach (IContent content in notification.PublishedEntities)
        {
            if (!content.ContentType.Alias.Equals(DocTypeAlias, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Invariant types have one value; variant types are checked only for the cultures in this publish.
            IEnumerable<string?> cultures = content.ContentType.VariesByCulture()
                ? content.AvailableCultures
                    .Where(c => notification.IsPublishingCulture(content, c))
                    .Cast<string?>()
                : new string?[] { null };

            foreach (string? culture in cultures)
            {
                if (Violates(content.GetValue<string>(PropertyAlias, culture)))
                {
                    notification.CancelOperation(new EventMessage(
                        "Publishing blocked",
                        $"'{content.Name}': {PropertyAlias} must not be written entirely in upper case.",
                        EventMessageType.Error));
                    return;
                }
            }
        }
    }

    // Replace this with the real rule. Default: non-empty text with letters but no lower-case ones.
    private static bool Violates(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Any(char.IsLetter) && !value.Any(char.IsLower);
}
