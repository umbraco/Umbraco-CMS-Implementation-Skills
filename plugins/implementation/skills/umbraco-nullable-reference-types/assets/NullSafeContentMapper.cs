using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Extensions;
using <Namespace>.Models;

namespace <Namespace>.Mapping;

/// <summary>
/// Maps <see cref="IPublishedContent"/> to <see cref="NullSafeContentViewModel"/> with no
/// null-forgiving operator (`!`) anywhere. Every value Umbraco hands back as `T?` is either guarded,
/// defaulted with `??`, or carried through as a nullable member — the three honest options.
///
/// Services are injected rather than reached through the "friendly" extension overloads that resolve
/// them from a static service locator, so this class is testable and works outside a web request.
/// Inject the DOCUMENT-specific IPublishedContentStatusFilteringService: the base
/// IPublishedStatusFilteringService is not registered in DI on its own.
/// </summary>
public sealed class NullSafeContentMapper
{
    private const string TitleAlias = "<TitleAlias>";
    private const string SubtitleAlias = "<SubtitleAlias>";
    private const string ImageAlias = "<ImageAlias>";
    private const string PickerAlias = "<PickerAlias>";

    private readonly IPublishedValueFallback _publishedValueFallback;
    private readonly IPublishedUrlProvider _publishedUrlProvider;
    private readonly IDocumentNavigationQueryService _navigationQueryService;
    private readonly IPublishedContentStatusFilteringService _publishedStatusFilteringService;

    public NullSafeContentMapper(
        IPublishedValueFallback publishedValueFallback,
        IPublishedUrlProvider publishedUrlProvider,
        IDocumentNavigationQueryService navigationQueryService,
        IPublishedContentStatusFilteringService publishedStatusFilteringService)
    {
        _publishedValueFallback = publishedValueFallback;
        _publishedUrlProvider = publishedUrlProvider;
        _navigationQueryService = navigationQueryService;
        _publishedStatusFilteringService = publishedStatusFilteringService;
    }

    public NullSafeContentViewModel Map(IPublishedContent content)
    {
        // Value<T>() is declared T? — null when the Document Type has no property with that alias, or
        // the stored value can't convert to T. A BLANK Textstring/Textarea is "", not null, so
        // `?? content.Name` would never fall back: IsNullOrWhiteSpace covers both. It also carries
        // [NotNullWhen(false)], so in the false branch the compiler already knows `title` is not
        // null: no `!` needed.
        string? title = content.Value<string>(_publishedValueFallback, TitleAlias);
        string resolvedTitle = string.IsNullOrWhiteSpace(title) ? content.Name : title;

        // defaultValue is ONLY applied when `fallback` includes Fallback.ToDefaultValue — passing it
        // alone does nothing. And even then the return type is still T?, so the `??` stays: it is what
        // satisfies the compiler, and it covers the converter returning null for a present value.
        string subtitle = content.Value<string>(
            _publishedValueFallback,
            SubtitleAlias,
            fallback: Fallback.ToDefaultValue,
            defaultValue: string.Empty) ?? string.Empty;

        // Parent() returns IPublishedContent? — null at the root. The IPublishedContent.Parent
        // property is [Obsolete] (removal in v18); use the extension. `?.` keeps the null as a
        // meaningful "no parent" in the model instead of inventing a value.
        string? parentName = content
            .Parent<IPublishedContent>(_navigationQueryService, _publishedStatusFilteringService)?
            .Name;

        // A single media picker converts to MediaWithCrops (an IPublishedContent). Nothing picked, or
        // the media was deleted, gives null — the pattern match both guards and narrows the type.
        string? imageUrl = content.Value<IPublishedContent>(_publishedValueFallback, ImageAlias)
            is { } image
                ? image.Url(_publishedUrlProvider)
                : null;

        // A single content picker converts to IPublishedContent?; it is null when nothing was picked
        // AND when the picked node has since been unpublished or deleted — so even a "mandatory"
        // picker must be treated as optional at read time.
        string? pickedContentName = content.Value<IPublishedContent>(_publishedValueFallback, PickerAlias)?.Name;

        return new NullSafeContentViewModel
        {
            Key = content.Key,
            Name = content.Name,
            Title = resolvedTitle,
            Subtitle = subtitle,
            ParentName = parentName,
            ImageUrl = imageUrl,
            PickedContentName = pickedContentName,
        };
    }
}
