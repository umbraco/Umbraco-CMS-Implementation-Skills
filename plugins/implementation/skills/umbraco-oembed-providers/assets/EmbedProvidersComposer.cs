using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Media.EmbedProviders;
using <Namespace>.EmbedProviders;

namespace <Namespace>;

// The first provider whose UrlSchemeRegex matches the pasted URL wins.
//   - Replace<Old, New>: swap a built-in (same position, same URLs).
//   - Insert<T>() / InsertBefore<Existing, T>(): take URLs a built-in also matches.
//   - Append<T>(): only for URLs no built-in matches.
// Keep only the lines for the providers you ship.
public class EmbedProvidersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.EmbedProviders()
            .Replace<YouTube, YouTubeNoCookieEmbedProvider>()
            .Append<CustomVideoEmbedProvider>()
            .Append<OEmbedJsonProviderTemplate>();
}
