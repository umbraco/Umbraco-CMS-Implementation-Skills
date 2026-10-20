using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco_CMS.Skills.TestHost;

/// <summary>
/// Deterministic runtime validation of umbraco-multilingual's additional-cultures composer
/// (assets/AllowAdditionalCulturesComposer.cs), compiled from the skill's asset and loaded into the
/// Clean host by its composer scan.
///
/// The iso code under test is the example's &lt;AdditionalIsoCode&gt; — a tag ICU doesn't predefine, so
/// .NET reports it as a UserCustomCulture. Each test first proves STOCK Umbraco rejects it; otherwise
/// the assertions after it would pass with or without the composer.
/// </summary>
[TestFixture]
public class AdditionalCulturesTests
{
    /// <summary>Must match the example's &lt;AdditionalIsoCode&gt; placeholder in .generate.json.</summary>
    private const string AdditionalIsoCode = "de-JP";

    private static IServiceProvider Services => ReferenceSiteFixture.Factory.Services;

    [SetUp]
    public void Stock_umbraco_rejects_the_culture()
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(AdditionalIsoCode);

        Assert.That(new IsoCodeValidator().IsValid(culture), Is.False,
            $"'{AdditionalIsoCode}' must be rejected by Umbraco's stock IsoCodeValidator on this platform, "
            + "or these tests prove nothing about the composer. Pick another non-predefined tag and "
            + "update both .generate.json and this constant.");
    }

    [Test]
    public void Registered_validator_accepts_the_additional_culture()
    {
        IIsoCodeValidator validator = Services.GetRequiredService<IIsoCodeValidator>();

        Assert.That(validator.IsValid(CultureInfo.GetCultureInfo(AdditionalIsoCode)), Is.True,
            "the composer must replace IIsoCodeValidator with one that accepts the listed culture");
        Assert.That(validator.IsValid(CultureInfo.GetCultureInfo("da-DK")), Is.True,
            "the replacement must still accept everything the stock validator accepts");
    }

    [Test]
    public void Culture_service_lists_the_additional_culture_for_the_languages_dropdown()
    {
        CultureInfo[] cultures = Services.GetRequiredService<ICultureService>().GetValidCultureInfos();

        Assert.That(cultures.Select(c => c.Name), Has.Some.EqualTo(AdditionalIsoCode),
            "ICultureService feeds Settings → Languages → Create; the culture must be listed there");
        Assert.That(cultures.Select(c => c.Name), Has.Some.EqualTo("da-DK"),
            "the stock cultures must still be listed");
    }

    /// <summary>The end goal: a language can actually be created for the culture.</summary>
    [Test]
    public async Task A_language_can_be_created_for_the_additional_culture()
    {
        ILanguageService languageService = Services.GetRequiredService<ILanguageService>();

        // The test database is reused across runs; an interrupted earlier run can leave the language
        // behind, and CreateAsync would then fail with DuplicateIsoCode on every later run.
        if (await languageService.GetAsync(AdditionalIsoCode) is not null)
        {
            await DeleteAsync(languageService);
        }

        Attempt<ILanguage, LanguageOperationStatus> created = await languageService.CreateAsync(
            new Language(AdditionalIsoCode, AdditionalIsoCode),
            Constants.Security.SuperUserKey);

        try
        {
            Assert.That(created.Success, Is.True,
                $"creating the '{AdditionalIsoCode}' language failed with {created.Status}");
        }
        finally
        {
            // Other fixtures share this host; leave its languages as the install left them.
            if (created.Success)
            {
                await DeleteAsync(languageService);
            }
        }
    }

    private static async Task DeleteAsync(ILanguageService languageService)
    {
        Attempt<ILanguage?, LanguageOperationStatus> deleted =
            await languageService.DeleteAsync(AdditionalIsoCode, Constants.Security.SuperUserKey);

        Assert.That(deleted.Success, Is.True,
            $"deleting the '{AdditionalIsoCode}' language failed with {deleted.Status}");
    }
}
