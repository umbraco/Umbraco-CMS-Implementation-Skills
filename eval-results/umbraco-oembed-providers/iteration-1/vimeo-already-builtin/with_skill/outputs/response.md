# Vimeo embeds in Umbraco 17: already built in

You don't need to add anything. Umbraco 17 already ships a Vimeo provider, `Umbraco.Cms.Core.Media.EmbedProviders.Vimeo`, and registers it by default. Editors can paste a Vimeo link into the Embed dialog and it will work as is.

Please don't add a custom provider. If you registered your own Vimeo provider with `builder.EmbedProviders().Append<MyVimeo>()`, it would never run. `IOEmbedService` uses the **first** registered provider whose regex matches, and the built-in Vimeo provider comes first. The only result would be dead code.

## What the built-in matches

These values come from `Umbraco.Core.dll` 17.5.3:

| | Value |
|---|---|
| URL regex (case-insensitive) | `^https?:\/\/(www\.)?vimeo\.com\/` |
| oEmbed endpoint | `https://vimeo.com/api/oembed.xml` |

Example URLs:

| Pasted URL | Works? |
|---|---|
| `https://vimeo.com/76979871` | Yes |
| `https://www.vimeo.com/76979871` | Yes |
| `https://vimeo.com/channels/staffpicks/76979871` | Yes, the regex matches (Vimeo's oEmbed API decides whether it resolves) |
| `https://player.vimeo.com/video/76979871` | **No**, the `player.` subdomain isn't in the regex |

## If it "doesn't work" for your editors

1. **Check which URL they paste.** Ask them to use the normal page URL (`https://vimeo.com/<id>`), not the `player.vimeo.com/video/<id>` link from Vimeo's embed code.
2. **Check whether the video is private or restricted.** The built-in calls Vimeo's public oEmbed API. Private or domain-restricted videos may return nothing, and the dialog then shows an invalid or empty result.
3. **Check the editor setup.** The Embed button has to be available in the editor, for example on the Rich Text Editor toolbar or as a Media Picker/embed option on the Data Type. That's content-modelling configuration, which this skill doesn't cover.
4. **Check outbound network access.** The server needs to reach `vimeo.com` (watch for firewalls and proxies).

## Only if you need different behaviour

You'd only need code if you want something the built-in doesn't do:

- **Accept `player.vimeo.com` URLs too, or change the markup** (for example `dnt=1`, Vimeo's do-not-track option): write your own provider and swap it in with `builder.EmbedProviders().Replace<Vimeo, MyVimeoEmbedProvider>()`. The replacement keeps the built-in's position in the list, so your provider is the one that answers.
- **Turn Vimeo embeds off:** `builder.EmbedProviders().Remove<Vimeo>()`.

Tell me if you want one of these and I'll write the provider and the composer.

**Verification:** I didn't change any code. I read the regex and endpoint from the 17.5.3 `Umbraco.Core.dll` in the local NuGet cache. I haven't run it against a live site, so I haven't checked that the built-in returns markup for specific videos.
