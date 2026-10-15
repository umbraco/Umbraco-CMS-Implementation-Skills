---
name: umbraco-oembed-providers
description: >
  Umbraco 17/18 embed (oEmbed) providers in C#: turn URLs pasted into the backoffice Embed dialog into
  markup. Covers new oEmbed providers (OEmbedProviderBase), hosts without oEmbed (custom IEmbedProvider
  markup), changing a built-in such as privacy-enhanced youtube-nocookie, and registration order in
  EmbedProviders. Use when asked to "add an embed provider", "support embedding from" a site, "oEmbed",
  "YouTube privacy or nocookie embed", "embed returns nothing for this URL", "my custom provider is
  ignored", or "embed our own video platform". SKIP: Rich Text Editor toolbar, Data Type or Content Type
  setup (content modelling); rendering content in templates (umbraco-templating); headless clients;
  non-Umbraco projects or Umbraco below 17.
---

# oEmbed providers

The backoffice Embed dialog sends the pasted URL to `IOEmbedService`, which asks the **first** registered provider whose regex matches for markup. Providers are C# classes registered in a composer.

## Route

Open [references/embed-providers.md](references/embed-providers.md), then the asset(s) its row names. Open another asset or fetch a doc only when you need an API or case they don't show.

| Task | Row in the reference |
|---|---|
| Site already built in? | *Choose the provider kind*, first row |
| Site has an oEmbed API | oEmbed API |
| Site has no oEmbed API | No oEmbed API |
| Change or override a built-in (YouTube nocookie, own markup) | Change a built-in |
| Custom provider ignored / wrong provider answers | *Registration and ordering* |
| Code copied from the docs doesn't compile or returns nothing | *Docs corrections* |

**Before writing any code, open the reference and start from its asset.** The traps are only covered there: the docs' v17 example doesn't compile, its `ApiEndpoint` doubles `?url=`, and `Append` can't override a built-in that matches the same URLs.

Version: read `Umbraco.Cms` from `.csproj`/`Directory.Packages.props`. Below 17 → skill doesn't apply, say so. No project → ask.

## Version compatibility

17 and 18: `IEmbedProvider` and `OEmbedProviderBase` are the same (`GetMarkupAsync` only). Assets compile on 17.5.3 and 18.2.0 with no warnings; runtime-verified on 17.5.3. 18 adds `protected virtual HttpClient GetHttpClient()` to `OEmbedProviderBase` (swap the client, e.g. for a proxy or tests).

## Best practices

The rules live in the reference: *Registration and ordering* (first match wins) and *Regex rules* (anchor, return `null` when unsupported). Treat the pasted URL as untrusted input.

## Coverage

**Asserted.** All `assets/` run in the `dotnet test` gate (`examples/oembed-providers/`, Clean host), offline: YouTube URLs render the nocookie player (proves `Replace` beat the built-in), unsupported URL → invalid result, custom host markup with encoded input, oEmbed template builds a single-`?url=` request.

## Validation

[`evals/evals.json`](evals/evals.json), run with `umbraco-skill-evaluator`.
