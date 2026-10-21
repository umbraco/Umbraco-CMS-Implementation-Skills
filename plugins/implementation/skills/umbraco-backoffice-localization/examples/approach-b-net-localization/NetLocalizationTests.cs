using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using Umbraco.Skills.Examples.BackofficeLocalization.NetLocalization;

namespace Umbraco_CMS.Skills.TestHost;

/// <summary>
/// Deterministic runtime validation of umbraco-backoffice-localization approach B: the skill's
/// en/da .user.xml files, read from config/lang by Umbraco's text service, and its
/// NotificationTextProvider registered by the skill's own composer.
///
/// Each test pins one claim the skill makes that the docs leave out or that a model gets wrong: a new
/// area is added, a core key is replaced only when its case is exact, tokens use %0%, a missing key
/// falls back to English, and an explicit culture beats the thread's.
/// </summary>
[TestFixture]
public class NetLocalizationTests
{
    /// <summary>Must match the example's &lt;NotificationArea&gt; placeholder in .generate.json.</summary>
    private const string Area = "skillsNotification";

    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo Danish = CultureInfo.GetCultureInfo("da-DK");
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de");

    private static IServiceProvider Services => ReferenceSiteFixture.Factory.Services;

    private static ILocalizedTextService Text => Services.GetRequiredService<ILocalizedTextService>();

    [Test]
    public void User_file_adds_a_new_area_per_language()
    {
        Assert.That(Text.Localize(Area, "body", English), Is.EqualTo("Your account is ready. Sign in to get started."));
        Assert.That(Text.Localize(Area, "body", Danish), Is.EqualTo("Din konto er klar. Log ind for at komme i gang."));
    }

    [Test]
    public void Tokens_use_percent_placeholders()
    {
        Assert.That(Text.Localize(Area, "subject", English, new[] { "Ada" }), Is.EqualTo("Welcome to the site, Ada"));
        Assert.That(Text.Localize(Area, "subject", Danish, new[] { "Ada" }), Is.EqualTo("Velkommen til sitet, Ada"));
    }

    // The two core-key tests read keys the harness adds to its copy of en.user.xml
    // (NetLocalizationExampleComposer.TestOnlyCoreOverrides), not the published asset.

    [Test]
    public void Core_key_is_replaced_when_the_alias_matches_exactly()
    {
        Assert.That(Text.Localize("login", "resetPasswordEmailCopySubject", English), Is.EqualTo("Skills: Reset your password"),
            "the exact core alias must replace 'Umbraco: Reset Password'");
    }

    [Test]
    public void Core_key_is_not_replaced_by_a_near_miss_alias()
    {
        // 'resetcodeexpired' differs from the core 'resetCodeExpired' only by case. It is a second key,
        // so the text a user would see stays untouched under the real alias.
        Assert.That(Text.Localize("login", "resetCodeExpired", English),
            Is.EqualTo("The link you have clicked on is invalid or has expired"),
            "a differently-cased alias must not replace the core text");
    }

    [Test]
    public void A_language_without_the_key_falls_back_to_the_english_text()
    {
        // de has core files but no user file, so the example's new area is missing for it and the
        // en-US document supplies it. This is why en.user.xml must hold every key.
        Assert.That(Text.Localize(Area, "subject", German, new[] { "Ada" }), Is.EqualTo("Welcome to the site, Ada"),
            "a missing key falls back to the English text");
    }

    [Test]
    public void A_culture_with_no_language_file_returns_the_key_in_brackets()
    {
        // Umbraco ships no Finnish file, so there is no document to fall back from.
        Assert.That(Text.Localize(Area, "subject", CultureInfo.GetCultureInfo("fi-FI")), Is.EqualTo("[subject]"));
    }

    [Test]
    public void Provider_uses_the_recipients_culture_not_the_threads()
    {
        var provider = Services.GetRequiredService<NotificationTextProvider>();
        CultureInfo original = Thread.CurrentThread.CurrentUICulture;
        try
        {
            Thread.CurrentThread.CurrentUICulture = English;
            Assert.That(provider.Subject(Danish, "Ada"), Is.EqualTo("Velkommen til sitet, Ada"),
                "an explicit culture must win over the thread's UI culture");
            Assert.That(Text.Localize(Area, "body"), Is.EqualTo("Your account is ready. Sign in to get started."),
                "the two-argument overload follows the thread's culture");

            Thread.CurrentThread.CurrentUICulture = Danish;
            Assert.That(Text.Localize(Area, "body"), Is.EqualTo("Din konto er klar. Log ind for at komme i gang."));
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = original;
        }
    }
}
