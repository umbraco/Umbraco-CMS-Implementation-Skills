using System.Globalization;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace <Namespace>;

public class NotificationTextProvider
{
    private const string Area = "<NotificationArea>";

    private readonly ILocalizedTextService _localizedTextService;

    public NotificationTextProvider(ILocalizedTextService localizedTextService)
        => _localizedTextService = localizedTextService;

    // Pass the recipient's culture. Without it Localize uses the current thread's UI culture, which in
    // a web request is the visitor's culture, not the culture of the person who receives the message.
    public string Subject(CultureInfo recipientCulture, string recipientName)
        => _localizedTextService.Localize(Area, "subject", recipientCulture, new[] { recipientName });

    public string Body(CultureInfo recipientCulture)
        => _localizedTextService.Localize(Area, "body", recipientCulture, Array.Empty<string>());
}
