# Adding cultures missing from the Languages dropdown

Use this **only** when a culture the site needs isn't offered in Settings → Languages → Create. Adding
an ordinary language needs no code; see [languages-and-variants.md](languages-and-variants.md).

## Why a culture goes missing

`ICultureService` builds the dropdown from `CultureInfo.GetCultures`, filtered through
`IIsoCodeValidator`, which rejects cultures flagged `CultureTypes.UserCustomCulture`. Under ICU (what
.NET 5+ uses on every OS, Windows included), a valid BCP 47 locale can drop out in two ways:

- **Not enumerated.** It resolves and passes the validator, but `GetCultures` never lists it. This is
  the case for the docs' own example, `zh-HK`: on .NET 10 it resolves, isn't custom, and isn't listed.
- **Flagged custom.** ICU has no data for it, so .NET reports it as `UserCustomCulture` and the
  validator rejects it. Don't steer users to a "similar" code without checking. `zh-Hant-HK` *is*
  listed, but it is flagged custom, so Umbraco rejects it too.

## Fix

Fetch [Adding Additional Languages](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/language-files/adding-additional-languages.md)
first. **The docs page is authoritative**: write the composer from it. If you can't fetch it, fall
back to [`assets/AllowAdditionalCulturesComposer.cs`](../assets/AllowAdditionalCulturesComposer.cs),
a copy of the docs' composer kept so this repo's runtime gate can compile and test it; where the two
differ, the docs win. Substitute `<Namespace>` and `<AdditionalIsoCode>`, the missing culture's
ISO code, e.g. `zh-HK`; add more array entries if several are missing. The composer replaces both
services with wrappers that accept the listed codes as well as everything the defaults accept.

**Which service actually needs replacing depends on which of the two causes applies.** ICU data varies
by version and OS, so check on the affected server's runtime (a scratch console app referencing
`Umbraco.Cms.Core` is enough):

```csharp
var culture = CultureInfo.GetCultureInfo("zh-HK");
bool listed = CultureInfo.GetCultures(CultureTypes.AllCultures).Any(c => c.Name == culture.Name);
bool accepted = new Umbraco.Cms.Core.Services.IsoCodeValidator().IsValid(culture);
```

- **Accepted but not listed** (e.g. `zh-HK`): only the dropdown is the problem. Replacing
  `ICultureService` alone is enough, and it's narrower because culture validation stays stock. Keep the
  stock validator by deleting the `IIsoCodeValidator` registration from the composer; the culture
  service registration already takes the validator from DI.
- **Rejected** (flagged `UserCustomCulture`): replace both, as the docs' composer does.

If you can't check, ship the docs' composer unchanged. It covers both cases.

1. Discover the project's namespace and composer folder convention.
2. Write the file with `<Namespace>` and `<AdditionalIsoCode>` substituted.
3. Build, restart, and check the culture now appears under Settings → Languages → Create.

## Caveats (tell the user)

- The validator is used for **all** culture validation, including backoffice user cultures, not just
  this dropdown. Only list codes the site genuinely needs.
- Only list real BCP 47 locales that `CultureInfo.GetCultureInfo` can resolve. The culture service
  silently drops a code it can't resolve, so the language still won't appear.
- This is **not** a workaround for inventing locales. If the business wants, say, "English for market
  X", prefer an existing real locale for that market, or a per-market root (see the note in
  [languages-and-variants.md](languages-and-variants.md)).
- This does not change the backoffice UI language (`DefaultUILanguage`) or add backoffice
  translations.

## Done

Tell the user which codes were added, where the composer lives, and that a restart is needed before
the culture shows in the dropdown.
