using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Skills.Examples.Fixtures;

namespace Umbraco.Skills.Examples.Multilingual.CultureVariants;

/// <summary>
/// This example's own harness — not part of the skill.
///
/// Does, in code, the steps the skill tells a user to do in the backoffice and that a package manifest
/// cannot carry:
///   1. set da-DK's fallback language (the importer reads CultureAlias/FriendlyName only);
///   2. create culture-variant content with per-culture names (so per-culture URL segments) and values;
///   3. assign a domain per culture (Culture and Hostnames) — relative "/en" and "/da", so they work on
///      whatever host the test server answers on;
///   4. publish each node in exactly the cultures it has;
///   5. give the About-us page block level variance: an INVARIANT Block List whose element type varies
///      by culture — block A exposed in both languages, block B exposed in English only.
///
/// The subtree hangs under site 2's SINGLE shared root rather than being a root of its own — a second
/// root would change every other example's URLs. The domains sit on the subtree's top node, so only
/// URLs beneath it gain an /en or /da prefix; nothing else on the site matches either.
///
/// Publishes node-by-node with explicit cultures rather than FixtureSite.PublishAndRefreshAsync's
/// PublishBranch(["*"]): on an already-published variant node "*" means the DEFAULT culture only, and
/// one child here deliberately has no Danish version at all.
///
/// Idempotent, because the test database is reused across boots.
/// </summary>
public class MultilingualContentSeeder : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public const string English = "en-US";
    public const string Danish = "da-DK";
    private const string PageType = "multilingualPage";

    // Must match ExampleFixtureContent.xml, which defines the element type with this key.
    private static readonly Guid BlockElementTypeKey = new("e3b5a8d1-6c2f-4a79-9d04-1f8b2c7e5a60");
    private static readonly Guid BlockAKey = new("b10c0000-0000-4000-8000-00000000000a");
    private static readonly Guid BlockBKey = new("b10c0000-0000-4000-8000-00000000000b");

    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly ILanguageService _languageService;
    private readonly IDomainService _domainService;
    private readonly IDocumentUrlService _documentUrlService;
    private readonly IDatabaseCacheRebuilder _cacheRebuilder;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ILogger<MultilingualContentSeeder> _logger;

    public MultilingualContentSeeder(
        IContentService contentService,
        IContentTypeService contentTypeService,
        ILanguageService languageService,
        IDomainService domainService,
        IDocumentUrlService documentUrlService,
        IDatabaseCacheRebuilder cacheRebuilder,
        IJsonSerializer jsonSerializer,
        ILogger<MultilingualContentSeeder> logger)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _languageService = languageService;
        _domainService = domainService;
        _documentUrlService = documentUrlService;
        _cacheRebuilder = cacheRebuilder;
        _jsonSerializer = jsonSerializer;
        _logger = logger;
    }

    public async Task HandleAsync(
        UmbracoApplicationStartedNotification notification,
        CancellationToken cancellationToken)
    {
        IContent? root = FixtureSite.FindRoot(_contentService);
        if (root is null)
        {
            _logger.LogWarning(
                "Site 2 has no root node, so the multilingual example content was not seeded.");
            return;
        }

        await EnsureDanishFallsBackToEnglishAsync();

        // name per culture → URL segment per culture. A null Danish name = no Danish version at all.
        IContent section = EnsureVariant(root, "Multilingual", "Flersproget",
            ("title", "Welcome", "Velkommen"));
        section.SetValue("code", "ML-001"); // invariant: shared across cultures
        _contentService.Save(section);

        IContent about = EnsureVariant(section, "About us", "Om os",
            ("title", "About us title", "Om os titel"));
        IContent fallback = EnsureVariant(section, "Fallback page", "Reserveside",
            ("title", "English fallback title", null));
        IContent englishOnly = EnsureVariant(section, "English only", null,
            ("title", "English only title", null));

        // The blocks property is invariant (no culture); per-culture content lives inside the blocks.
        about.SetValue("blocks", _jsonSerializer.Serialize(BuildVariantBlocks()));
        _contentService.Save(about);

        foreach (IContent node in new[] { section, about, fallback, englishOnly })
        {
            PublishResult result = _contentService.Publish(node, node.AvailableCultures.ToArray());
            if (!result.Success)
            {
                throw new InvalidOperationException(
                    $"Publishing multilingual fixture node '{node.Name}' failed with {result.Result}.");
            }
        }

        var domains = await _domainService.UpdateDomainsAsync(section.Key, new DomainsUpdateModel
        {
            Domains =
            [
                new DomainModel { DomainName = "/en", IsoCode = English },
                new DomainModel { DomainName = "/da", IsoCode = Danish },
            ],
        });
        if (!domains.Success)
        {
            throw new InvalidOperationException(
                $"Assigning /en and /da domains to the multilingual fixture failed with {domains.Status}.");
        }

        // See FixtureSite.PublishAndRefreshAsync for why both are needed after startup publishing.
        await _documentUrlService.RebuildAllUrlsAsync();
        await _cacheRebuilder.RebuildAsync(useBackgroundThread: false);
    }

    /// <summary>
    /// Block A has a title in both languages and is exposed in both; block B has only an English title
    /// and is exposed only in English, so it is "unexposed" in Danish. blockCode is invariant.
    /// </summary>
    private static BlockListValue BuildVariantBlocks() =>
        new([new BlockListLayoutItem(BlockAKey), new BlockListLayoutItem(BlockBKey)])
        {
            ContentData =
            [
                new BlockItemData(BlockAKey, BlockElementTypeKey, "multilingualBlock")
                {
                    Values =
                    [
                        new BlockPropertyValue { Alias = "blockTitle", Value = "Block A", Culture = English },
                        new BlockPropertyValue { Alias = "blockTitle", Value = "Blok A", Culture = Danish },
                        new BlockPropertyValue { Alias = "blockCode", Value = "A-1" },
                    ],
                },
                new BlockItemData(BlockBKey, BlockElementTypeKey, "multilingualBlock")
                {
                    Values =
                    [
                        new BlockPropertyValue { Alias = "blockTitle", Value = "Block B", Culture = English },
                        new BlockPropertyValue { Alias = "blockCode", Value = "B-2" },
                    ],
                },
            ],
            Expose =
            [
                new BlockItemVariation(BlockAKey, English, null),
                new BlockItemVariation(BlockAKey, Danish, null),
                new BlockItemVariation(BlockBKey, English, null),
            ],
        };

    private async Task EnsureDanishFallsBackToEnglishAsync()
    {
        ILanguage danish = await _languageService.GetAsync(Danish)
            ?? throw new InvalidOperationException(
                $"'{Danish}' is missing — the skill's multilingual-package.xml should have imported it.");

        if (danish.FallbackIsoCode == English)
        {
            return;
        }

        danish.FallbackIsoCode = English;
        var updated = await _languageService.UpdateAsync(danish, Constants.Security.SuperUserKey);
        if (!updated.Success)
        {
            throw new InvalidOperationException($"Setting {Danish}'s fallback failed with {updated.Status}.");
        }
    }

    /// <summary>
    /// Finds a child of <paramref name="parent"/> by its English name, or creates it; either way sets
    /// the per-culture names and values, so a changed fixture converges on reboot.
    /// </summary>
    private IContent EnsureVariant(
        IContent parent,
        string englishName,
        string? danishName,
        (string Alias, string English, string? Danish) value)
    {
        // The shorter GetPagedChildren overload is obsolete, so pass every parameter explicitly.
        IContent node = _contentService
            .GetPagedChildren(parent.Id, 0, 100, out _, propertyAliases: null, filter: null, ordering: null)
            .FirstOrDefault(c => c.GetCultureName(English) == englishName)
            ?? _contentService.Create(englishName, parent.Id, PageType);

        IContentType? contentType = _contentTypeService.Get(PageType);
        if (contentType?.DefaultTemplate is not null)
        {
            node.TemplateId = contentType.DefaultTemplate.Id;
        }

        node.SetCultureName(englishName, English);
        node.SetValue(value.Alias, value.English, English);

        if (danishName is not null)
        {
            node.SetCultureName(danishName, Danish);
            node.SetValue(value.Alias, value.Danish, Danish);
        }

        _contentService.Save(node);
        return node;
    }
}

/// <summary>Registers the seeder. Picked up by AddComposers() in the blank host's Program.cs.</summary>
public class ExampleHostWiringComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, MultilingualContentSeeder>();
}
