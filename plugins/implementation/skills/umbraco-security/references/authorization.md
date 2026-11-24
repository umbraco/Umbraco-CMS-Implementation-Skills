# Members, access and permissions

## Fetch first (pick the page for the task)

| Task | Docs page |
|---|---|
| Member registration, login and member-only pages on a Razor site | [Member registration and login](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/tutorials/members-registration-and-login.md) |
| Current member in code | [IMemberManager](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/templating-and-rendering/querying/imembermanager.md) |
| Delivery API keys and public access | [Content Delivery API](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/headless-and-apis/content-delivery-api.md) |
| Members on a headless front end | [Protected content in the Delivery API](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/headless-and-apis/content-delivery-api/protected-content-in-the-delivery-api.md) and [server-to-server access](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/headless-and-apis/content-delivery-api/protected-content-in-the-delivery-api/server-to-server-access.md) |
| User groups and permissions | [Users](https://docs.umbraco.com/umbraco-cms/manage-and-publish-content/users-and-members/users.md) and [Sensitive data](https://docs.umbraco.com/umbraco-cms/run-in-production/security/sensitive-data-on-members.md) |
| Machine access to the Management API | [API users](https://docs.umbraco.com/umbraco-cms/manage-and-publish-content/users-and-members/users/api-users.md) |

## Member login on Razor sites

- Use the built-in surface controllers (`UmbLoginController`, `UmbRegisterController`) through
  `Html.BeginUmbracoForm`. They bring antiforgery validation, lockout and the member sign-in
  manager; a hand-rolled login that checks passwords through `IMemberService` loses all of that.
- A custom login or registration action that accepts a return URL must check `Url.IsLocalUrl`,
  or it becomes an open redirect.
- New members must be approved to sign in, and group assignment on registration is code (a
  `MemberSavedNotification` handler). Without it, group-based public access never matches a new
  member. Member password rules are `Security:MemberPassword`, separate from users'.

## Member-protected content

- Public access is set per content node (group-based or member-based), with a login page and a
  no-access page. Children inherit it.
- **Public access does not protect media or other static files.** `/media/...` and `wwwroot` are
  served by the static file middleware, which runs before routing and authentication. If files
  must be private, store them outside `wwwroot` and the media path and serve them through a
  controller that checks the member.
- In navigation, use `IMemberManager.IsProtectedAsync(path)` and `MemberHasAccessAsync(path)`.
- Protected pages must not be served from a shared output cache or CDN.

## Member-only controllers and endpoints

Put `[UmbracoMemberAuthorize]` on surface and API controllers that act for a member. Its
constructor takes three comma-delimited lists, **in this order**: `allowType` (member type
aliases), `allowGroup` (member groups), `allowMembers` (member IDs). So
`[UmbracoMemberAuthorize("", "Premium", "")]` allows the Premium group only. Take the member from
`IMemberManager.GetCurrentMemberAsync()`, never from a query-string or form value.

## Delivery API access

- `DeliveryApi:PublicAccess: false` makes the whole API require the `Api-Key` header. The same
  key also unlocks **preview (draft) content**, so it is a server-side secret: never ship it in
  browser JavaScript or a mobile app. Show previews through the front end's server side.
- Keep internal document types out with `DisallowedContentTypeAliases`.
- **Protected content is excluded by default.** Members reach it with a member access token:
  enable `DeliveryApi:MemberAuthorization:AuthorizationCodeFlow` with **absolute**
  `LoginRedirectUrls` (no wildcards), restart, then **rebuild `DeliveryApiContentIndex`**, the
  commonly missed step. The client uses Authorization Code + PKCE with `client_id`
  `umbraco-member`. The docs warn against public clients in web apps: for a server-rendered front
  end, prefer a server-side flow or server-to-server access.

## Backoffice permissions and API users

- Grant through **user groups** (sections, start nodes, granular permissions), never to
  individuals. Only administrators should reach Settings and Users. Keep the "Sensitive data"
  group small: it reveals member fields marked sensitive.
- A script, CI job or integration calling the Management API uses an **API user** with Client
  Credentials in a narrowly scoped group, never a person's login. Reuse its token until it
  expires.

**Related:** [cors.md](cors.md) (CORS is not access control).
