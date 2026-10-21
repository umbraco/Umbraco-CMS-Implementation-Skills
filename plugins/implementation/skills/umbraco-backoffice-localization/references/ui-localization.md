# Approach A: UI localization (App_Plugins manifest + JS modules)

## When to choose this approach

Choose it for any text drawn in the backoffice: labels of your own extension, Document, Media and
Member Type names, tabs, groups, property labels and descriptions, validation messages, or a change to
Umbraco's own wording. For strings the server builds and sends (emails, login errors, health checks)
use [net-localization.md](net-localization.md) instead.

## Fetch the docs first

The shipped assets follow these pages. Read them before writing, since the manifest shape is theirs:

- [Localization extension type](https://docs.umbraco.com/umbraco-cms/extend-your-project/backoffice-extensions/extending-overview/extension-types/localization.md): manifest, JS module, inline `meta.localizations`
- [Backoffice localization](https://docs.umbraco.com/umbraco-cms/extend-your-project/backoffice-extensions/foundation/localization.md): `umb-localize`, `this.localize.term`, arguments, placeholders, `#` in manifests
- [Document Type localization](https://docs.umbraco.com/umbraco-cms/model-your-content/content-types-and-structure/data/defining-content/document-type-localization.md): `#area_key` and `{#area_key}` in the Document Type editor (also Media and Member Types)
- [Adding localization to the dashboard](https://docs.umbraco.com/umbraco-cms/extend-your-project/tutorials/creating-a-custom-dashboard/adding-localization-to-the-dashboard.md)

## Steps

1. **Find the project.** Look for `App_Plugins/` in the web project. If the package already has an
   `umbraco-package.json`, add `localization` entries to it instead of creating a second manifest.
2. **Write the manifest** from [`assets/umbraco-package.json`](../assets/umbraco-package.json), with
   `<PackageName>` replaced. It goes at `App_Plugins/<PackageName>/umbraco-package.json`, never in a
   subfolder of that. One `localization` extension per culture.
3. **Write one JS module per culture** from [`assets/en.js`](../assets/en.js) and
   [`assets/da.js`](../assets/da.js), next to the manifest and referenced by the `js` path. Each is
   `export default { area: { key: 'text' } }`. Start from English and make it the complete set.
4. **Apply the keys.**
   - In a manifest (dashboard `meta.label`, section name and so on): `"#area_key"`.
   - In the Document Type editor, as a name, tab, group, property name or description:
     `#area_key`. A property **description** uses braces instead, `{#area_key}`, because
     descriptions are Markdown and `#` would start a heading. A custom **validation message** is
     plain text, so it uses `#area_key` like a name, not the braces the docs sample shows.
   - In your own element: `<umb-localize key="area_key">English default</umb-localize>` or
     `this.localize.term('area_key')`.
5. **Tell the user** that Umbraco must be restarted for a new or changed manifest, that later text
   edits need only a browser reload, and that the text shown follows each user's **UI Culture**
   setting (a user can change it under their profile).
6. **Build** if a .NET project is present, and report whether you ran it.

## Rules the docs leave out

These come from the backoffice source (`localization.registry.ts`), not the docs pages above.
Confirm against the pinned Umbraco version if a result looks different.

- **Keys merge across extensions.** All localization extensions for the same culture are merged into
  one dictionary, so a package can add keys without replacing anyone's file.
- **Lower `weight` wins.** Extensions are applied from highest weight to lowest, so the lowest weight
  is applied last and overrides. The default weight is 100 and Umbraco's own translations use it.
  To change core wording give your extension a weight **below 100** (the asset uses 50) and reuse the
  core key, for example `actions_assigndomain`. At equal weight the winner is not defined.
- **Override core wording in every UI culture your editors use.** A key is looked up in the user's
  regional culture, then their language, and only then in `en`. Umbraco ships translations of most
  core keys (Danish has `actions_assigndomain: 'Tilføj domæne'`), so an `en`-only override never
  reaches a Danish editor. Add an extension per culture, as the asset does for `en` and `da`.
- **Validation messages are not Markdown.** They are rendered by replacing each `#area_key` with its
  text and nothing else, so `{#area_key}` there shows the translation wrapped in literal braces.
  Only descriptions (Umbraco Flavored Markdown) take the brace form.
- **Which culture files load.** For a user whose UI culture is `da-DK`, extensions with culture
  `da-DK` or `da` load, plus `en` always. A file registered as `da-DK` is not used for plain `da`.
  Register the short language code unless you need a regional difference.
- **Missing keys.** A key absent in the user's language falls back to `en`. If it is absent there
  too, `umb-localize` shows its inner text; add `debug="true"` to show the key instead while testing.
- **Inline text.** `meta.localizations` accepts strings only, grouped one level deep
  (`{ "area": { "key": "text" } }`); keys placed directly under `localizations`, or nested deeper, do
  not work. Function values (computed text) need a JS module.
- **Arguments.** Placeholders `{0}` or `%0%` in a string are replaced in order by the `args` attribute
  or the extra arguments to `term()`. Use a function value when the text must change with the value.
- **Copy the core key exactly.** Keys are case-sensitive and the wording the user sees ("Culture and
  Hostnames") is not the key. Read the shipped English file: in a restored project it is
  `~/.nuget/packages/umbraco.cms.staticassets/<version>/staticwebassets/umbraco/backoffice/assets/lang/en.ts`
  (the Umbraco-CMS source has it at `src/Umbraco.Web.UI.Client/src/assets/lang/en.ts`). The area is
  the group the line sits in. "Culture and Hostnames" is `actions_assigndomain`, all lowercase. A near
  miss such as `assignDomain` does not override anything; it just adds an unused key.
- **Core override is a separate package.** Put overrides in their own folder and manifest
  ([`assets/core-overrides-package.json`](../assets/core-overrides-package.json)) so the package that
  owns your labels is not tied to wording changes.

## Done summary

List the files created and their paths, the keys and where each is used, the cultures covered, the
restart/reload instructions, and what you did not run (for example "not opened in a backoffice").
