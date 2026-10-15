using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Acme.Web.Services;

// Sends the "your editor account is ready" email when a new backoffice user is created.
// Editors have a UI language on their profile (IUser.Language, for example "da" or "en-US").
public class WelcomeEmailSender
{
    private readonly IEmailSender _emailSender;
    private readonly ILocalizedTextService _localizedTextService;

    public WelcomeEmailSender(IEmailSender emailSender, ILocalizedTextService localizedTextService)
    {
        _emailSender = emailSender;
        _localizedTextService = localizedTextService;
    }

    public async Task SendAsync(IUser user)
    {
        // TODO: the subject and body are English only. Danish and German editors should get their own language.
        var subject = _localizedTextService.Localize("welcome", "subject");
        var body = _localizedTextService.Localize("welcome", "body");

        var message = new EmailMessage(null, user.Email, subject, body.Replace("{0}", user.Name), isBodyHtml: false);
        await _emailSender.SendAsync(message, emailType: "Welcome");
    }
}
