---
name: umbraco-backoffice-localization
description: >
  Use for Umbraco 17+ work on the language editors see inside the backoffice, not the language of
  the website. Covers translating a custom package, dashboard, section or other extension's labels
  into the editor's UI language; localizing Document Type, Media Type and Member Type names, tabs,
  groups, property labels, descriptions and validation messages with `#area_key`; overriding
  Umbraco's own wording (for example renaming "Culture and Hostnames") or adding a missing
  translation; backoffice text showing raw keys or `[someKey]` in brackets; a localization manifest
  that isn't picked up; and server-side text such as emails, login errors and health checks
  translated with `ILocalizedTextService` and `config/lang` `.user.xml` files. SKIP: non-Umbraco
  apps, Umbraco 13 and earlier, translating website or visitor-facing content, languages, variants,
  hreflang or dictionary items (use umbraco-multilingual), and writing the extension itself.
---

# Backoffice localization

Two separate systems translate what editors see. They use different files, different key syntax and
different fallback rules, so identify which text you are translating before writing anything.

| | Reference | Where the text appears | Files |
|---|---|---|---|
| **A** | [ui-localization.md](references/ui-localization.md) | Everything drawn in the backoffice UI: extension labels, Document/Media/Member Type names, tabs, groups, properties, core wording | `App_Plugins/<Package>/umbraco-package.json` + a JS module per culture ([assets](assets/)) |
| **B** | [net-localization.md](references/net-localization.md) | Text built on the server with no UI: emails, login errors, health checks | `config/lang/<culture>.user.xml`, read through `ILocalizedTextService` ([assets](assets/)) |

**How to decide.** Default to **A**: nearly all backoffice text is UI localization, and `.NET`
localization is only for strings the server composes itself. Use **B** when the string is produced in
C# and sent out (an email subject, an error from a login flow, a health check message). A request can
need both: say which part each file serves. Open the chosen reference and start from its assets before
writing any file.

Website content in several languages (variants, domains, dictionary items) is a different feature,
handled by the `umbraco-multilingual` skill. If the same strings must exist for visitors, do not put
them in these files. If you have to answer a visitor-text question here, view text comes from
dictionary items, read with `Umbraco.GetDictionaryValue(key)` or
`Umbraco.GetDictionaryValueOrDefault(key, defaultText)`; `GetDictionaryValue(key, "text")` does not
exist, and Razor views are not compiled by `dotnet build`, so that mistake only shows at runtime.

## Version compatibility

Umbraco 17+. The UI system is the extension-registry `localization` type (Umbraco 14 and later);
`.user.xml` overrides for server-side text are far older and unchanged.

## Best practices

- Keep UI keys grouped as `area` then `key`. Every key is requested as `area_key`, and only one
  grouping level exists. Prefix areas with your package name so they cannot collide with core areas,
  in camelCase with letters, digits and `_` only (`acmeWelcome`, not `Acme.Welcome`): a `#area_key`
  reference stops matching at a `.` or `-`.
- Ship an `en` file as the complete set. English is the fallback for every missing UI key, so other
  cultures can be partial.
- Server-side text falls back to the **English (`en-US`) file only**, so `en.user.xml` must hold every
  key you use. A key missing from English too, or a culture with no language file at all, returns
  `[key]`. Give keys distinctive aliases: a key missing from your area is first looked up by alias in
  every other area, so a generic one (`publish`) can pick up unrelated core text.
- Register a manifest only at `App_Plugins/` or `App_Plugins/<Package>/`. A deeper folder is ignored
  without any error.
- Do not edit the core language files. Override with your own extension (UI) or `.user.xml` (server).
- Tell the user a restart is needed after registering or renaming a manifest, a browser reload after
  editing a JS text, and a restart after editing a `.user.xml`.

## Reply checklist

End your reply to the user with these, each as its own sentence (a code comment does not count):

1. **Fallback.** UI text: a key missing in the user's language shows the `en` text, and if `en` lacks it
   too the element's inner text. Server text: a key missing in one language shows the English text, so
   the English file must hold every key, and a key missing from English, or a language with no file at
   all, shows `[key]`, for example `[subject]`.
2. **Coverage.** Which languages you set up and what happens for any other.
3. **Restart or reload.** What the user must do for the change to show.
4. **What you did not run.** Only claim a build or a rendered backoffice if you ran one.

## Validation

`evals/evals.json` holds the scenarios and checkable expectations;
`evals/README.md` explains how they are run.
