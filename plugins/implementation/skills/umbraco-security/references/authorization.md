# Authorization

Who can reach what, once identity is settled: member-only pages, member-only endpoints, Delivery
API access, and what editors may do in the backoffice.

## Fetch first

- **Public access** (protecting content for members or member groups): the "Member-only pages"
  section of [Member registration and login](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/tutorials/members-registration-and-login.md)
- **Delivery API access:** `PublicAccess` and `ApiKey` in
  [Content Delivery API](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/headless-and-apis/content-delivery-api/README.md)
- **Backoffice permissions:** [Users](https://docs.umbraco.com/umbraco-cms/manage-and-publish-content/users-and-members/users/README.md)
  (user groups, sections, start nodes, granular permissions) and
  [Sensitive data](https://docs.umbraco.com/umbraco-cms/run-in-production/security/sensitive-data-on-members.md)

## Member-protected content

- Public access is set per content node in the backoffice (**Public access**: group-based or
  member-based), with a login page and a no-access page. Children inherit it. Group-based rules
  are easier to maintain than lists of individual members.
- **Public access does not protect media or other static files.** Umbraco serves `/media/...`, and
  anything under `wwwroot`, through the static file middleware, which runs before routing and
  authentication. A PDF linked from a protected page can be downloaded by anyone who has the URL.
  If files must be private, serve them through a controller that checks the member (below), and
  store them outside `wwwroot` and the public media path.
- Hiding a link is not protection, but showing links members can't open is poor UX. In
  navigation, use `IMemberManager.IsProtectedAsync(path)` and `MemberHasAccessAsync(path)` (with
  overloads that take many paths, to avoid N calls).
- Protected pages must not be served from a shared output cache or CDN.

## Member-only controllers and endpoints

Put `[UmbracoMemberAuthorize]` on surface controllers and API controllers that act for a member.
Its constructor takes three comma-delimited lists, **in this order**: `allowType` (member type
aliases), `allowGroup` (member groups), `allowMembers` (member IDs). For example,
`[UmbracoMemberAuthorize("", "Premium", "")]` allows the Premium group only. It returns 401 or 403
for API controllers, and redirects for MVC/surface controllers. `[AllowAnonymous]` on an action
opts that action out. Never check a member's group by hand from a query-string or form value.

## Delivery API access

- **Protected content is excluded by default.** It only appears with member authorization enabled
  and a member access token (see [authentication-members.md](authentication-members.md)).
- `DeliveryApi:PublicAccess: false` makes the entire API require the `Api-Key` header. The same
  `ApiKey` also unlocks **preview (draft) content**, so treat it as a server-side secret. Never
  ship it in browser JavaScript or a mobile app binary.
- Use `AllowedContentTypeAliases` / `DisallowedContentTypeAliases` to keep internal document types
  (settings nodes, form configuration) out of the API completely. That beats relying on obscurity.

## Backoffice permissions

- Grant through **user groups**: sections, content and media start nodes, document-level granular
  permissions. Don't grant to individuals. Give editors the least access they need. Only
  administrators should reach Settings and Users.
- Member fields marked **sensitive** are hidden from users outside the "Sensitive data" group.
  Use this for personal data (date of birth, addresses) and keep that group small.

**Related:** [cors.md](cors.md) (CORS is not authorization),
[authentication-users.md](authentication-users.md).
