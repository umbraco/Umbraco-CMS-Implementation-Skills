# Approach B: .NET localization (`config/lang` user files + `ILocalizedTextService`)

## When to choose this approach

Choose it only for text the server composes with no accompanying UI: email subjects and bodies,
messages from the login flow, health check results. Anything shown inside the backoffice UI is
[UI localization](ui-localization.md). Using `.user.xml` to translate a dashboard label does nothing.

## Fetch the docs first

- [.NET localization](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/language-files/net-localization.md): file location, csproj entry, `ILocalizedTextService`, missing keys
- [Language files overview](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/language-files.md): the two systems and the languages Umbraco ships

## Steps

1. **Create `config/lang/` at the web project root.** It does not exist on a clean install. On Umbraco
   Cloud it goes in the `src` project.
2. **Write one `<culture>.user.xml` per language** from [`assets/en.user.xml`](../assets/en.user.xml)
   and [`assets/da.user.xml`](../assets/da.user.xml). The file uses the same `<language><area><key>`
   layout as the core files. The name is the language code (`en.user.xml`, `da.user.xml`) or the full
   culture (`da-DK.user.xml`).
3. **Make the files deploy.** Add to the web project's `.csproj` so `dotnet publish` copies them:
   ```xml
   <ItemGroup>
       <Content Include="config/**" CopyToOutputDirectory="Always" />
   </ItemGroup>
   ```
4. **Read the text** with `ILocalizedTextService`, from
   [`assets/NotificationTextProvider.cs`](../assets/NotificationTextProvider.cs), registered by
   [`assets/NotificationTextComposer.cs`](../assets/NotificationTextComposer.cs). Replace
   `<Namespace>` and `<NotificationArea>` (the `area` alias used in the XML). Where the culture comes
   from a user setting (`IUser.Language` and the like), map it to one of the languages you ship files
   for and send everything else, including an empty value, to the English default. A valid culture you
   have no file for (French, say) would otherwise render `[key]`.
5. **Tell the user** to restart: the files are read once and cached.

## Rules the docs leave out

These come from the Umbraco source (`LocalizedTextServiceFileSources`, `LocalizedTextService`).

- **Fallback is to English only.** A culture that lacks a key gets the text from the `en-US` file
  (`en.user.xml` is read for `en-US` as well), not from your default language. If English lacks the key
  too, the result is `[key]` (for example `[subject]`), and a culture with no language file at all also
  returns `[key]` and logs a warning. So put every key in `en.user.xml`; other languages may be partial.
- **`.user.xml` overrides, other files only add.** Keys in `config/lang` replace core keys of the same
  area and alias. Keys in a plugin's own `App_Plugins/<Package>/lang/<culture>.xml` are only added and
  never replace a core key, so a package should use its own area. A whole new area is simply added.
- **Copy the core key exactly.** Alias matching is case-sensitive. Take the area and `alias` from
  [`en.xml` in the Umbraco-CMS source](https://github.com/umbraco/Umbraco-CMS/blob/main/src/Umbraco.Core/EmbeddedResources/Lang/en.xml), not from memory or from
  the docs sample. The reset-password email subject, for example, is area `login`, alias
  `resetPasswordEmailCopySubject`; `resetPasswordEmailCopysubject` is a near miss that adds a second,
  unused key, and the core text stays.
- **Culture is explicit or the thread's.** The two-argument `Localize(area, key)` uses the current
  thread's UI culture, which in a web request is the visitor's request culture, not the editor's
  backoffice language. For a message to a known recipient, pass that person's culture
  (`Localize(area, key, culture, tokens)`).
- **Cultures need ICU on the server.** `CultureInfo.GetCultureInfo("da-DK")` throws
  `CultureNotFoundException` when the app runs with invariant globalization
  (`<InvariantGlobalization>true</InvariantGlobalization>` or `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`,
  as in some slim or Alpine container images). When mapping a recipient's language to a culture for a
  Linux deployment, say that ICU must be present, or guard the lookup.
- **Tokens use `%0%`.** Write `%0%`, `%1%` in the XML and pass `new[] { "Ada" }`; `{0}` is the UI
  system's placeholder and is not replaced here.
- **File naming.** The language part must match the target language file by culture name or
  two-letter code; an unrelated name is ignored.

## Done summary

List the XML files and the keys, the csproj change, how the service is registered and used, the
restart instruction, and that you did not send a real email or open the backoffice unless you did.
State the fallback outright so the user knows what a gap looks like: a key missing from one language
shows the English text, and a key missing from English, or a language with no file, shows `[key]`.
Name which languages are covered and what the code does for any other.
