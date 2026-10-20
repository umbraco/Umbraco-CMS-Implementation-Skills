using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace <Namespace>;

// From the official docs: "Adding Additional Languages"
// https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/language-files/adding-additional-languages.md
// Only the usings differ — added so the file compiles standalone without project-wide global usings.

/// <summary>
/// Replaces the default <see cref="IIsoCodeValidator"/> and <see cref="ICultureService"/>
/// to allow specific cultures that are valid BCP 47 locales but get incorrectly flagged as
/// <see cref="CultureTypes.UserCustomCulture"/> on .NET with app-local ICU, or are not
/// enumerated by <see cref="CultureInfo.GetCultures"/> despite being resolvable.
/// </summary>
public class AllowAdditionalCulturesComposer : IComposer
{
    // The cultures the site needs that are missing from Settings → Languages → Create — the docs'
    // example is ["zh-HK"]. Add more entries as needed. The validator is used for ALL culture
    // validation in Umbraco, so list only what's genuinely needed.
    private static readonly string[] _additionalIsoCodes = ["<AdditionalIsoCode>"];

    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddUnique<IIsoCodeValidator>(
            new AllowAdditionalCulturesIsoCodeValidator(_additionalIsoCodes));
        builder.Services.AddUnique<ICultureService>(provider =>
            new AllowAdditionalCulturesCultureService(
                provider.GetRequiredService<IIsoCodeValidator>(),
                _additionalIsoCodes));
    }
}

internal class AllowAdditionalCulturesIsoCodeValidator : IIsoCodeValidator
{
    private readonly IsoCodeValidator _inner = new();
    private readonly HashSet<string> _additionalIsoCodes;

    public AllowAdditionalCulturesIsoCodeValidator(params string[] additionalIsoCodes)
        => _additionalIsoCodes = new HashSet<string>(additionalIsoCodes, StringComparer.OrdinalIgnoreCase);

    public bool IsValid(CultureInfo culture)
        => _inner.IsValid(culture) || _additionalIsoCodes.Contains(culture.Name);
}

internal class AllowAdditionalCulturesCultureService : ICultureService
{
    private readonly CultureService _inner;
    private readonly string[] _additionalIsoCodes;

    public AllowAdditionalCulturesCultureService(
        IIsoCodeValidator isoCodeValidator,
        string[] additionalIsoCodes)
    {
        _inner = new CultureService(isoCodeValidator);
        _additionalIsoCodes = additionalIsoCodes;
    }

    public CultureInfo[] GetValidCultureInfos()
    {
        CultureInfo[] baseCultures = _inner.GetValidCultureInfos();

        // Resolve and append any additional cultures not already in the list.
        var existing = new HashSet<string>(baseCultures.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);

        IEnumerable<CultureInfo> extra = _additionalIsoCodes
            .Where(code => !existing.Contains(code))
            .Select(code =>
            {
                try { return CultureInfo.GetCultureInfo(code); }
                catch (CultureNotFoundException) { return null; }
            })
            .Where(c => c is not null)
            .Cast<CultureInfo>();

        return baseCultures
            .Concat(extra)
            .OrderBy(c => c.EnglishName)
            .ToArray();
    }
}
