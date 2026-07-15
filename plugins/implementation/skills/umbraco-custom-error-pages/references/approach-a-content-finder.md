# Approach A — `IContentLastChanceFinder` (custom, default)

A code-based 404 handler. This is the docs' "Advanced" path
([Implement Custom Error Pages](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/tutorials/custom-error-page)),
but the asset here is an **improved variant, not the docs' sample**: instead of hardcoding a
page GUID (which differs per environment), it resolves the error page structurally —
`request.Domain → site root → FirstChild(ErrorPageAlias)` — so the same code works across
environments and resolves the correct root per site in multi-site setups.

## When to choose this over Approach B

- **Multi-site by domain** — Approach B's config can't resolve per-domain roots.
- The team prefers everything in source control with no per-environment GUID config.
- Dynamic logic is needed (custom fallbacks, logging, etc.).

If none of those apply and the team wants zero custom code, offer
[Approach B](approach-b-config-based.md) (the docs' recommended path) instead.

## Steps

### 1. Discovery

Search the project for: root namespace, Umbraco version (must be 16.1+), and existing
`ContentFinders/` / `Composers/` / `Controllers/` folder conventions.

Ask the user:
1. **Which error pages?** (404, 500, or both)
2. **Where will the error page live in the content tree?** — the finder's `FirstChild()`
   navigation must match (see Custom structure below).

### 2. Create the Document Type + content node (backoffice)

Create a **Document Type with Template** named *Error Page 404* (alias `ErrorPage404`) with
`heading` (Textstring) and `message` (Rich Text) properties, then create and **publish** a
content node of that type in the agreed location. The published node must exist before the
page will display.

Prefer the [Umbraco Developer MCP](https://docs.umbraco.com/umbraco-in-ai/mcp/cms-developer-mcp)
to do this directly if it's available; otherwise walk the user through the steps manually in
the backoffice UI, one at a time.

### 3. Write the finder

Read [`../assets/PageNotFoundContentFinder.cs`](../assets/PageNotFoundContentFinder.cs) and
replace `<Namespace>` with the project namespace and `<ErrorPageAlias>` with `ErrorPage404`.
Place it in `ContentFinders/` if that folder exists (or create it); the composer class is in
the same file and registers automatically.

If a real `.csproj` is present, run `dotnet build` afterwards and fix any errors. If no
buildable project exists, say so and state whether the code is expected to compile against
the documented APIs used — don't stay silent on build correctness.

### Custom structure

If the error page isn't a direct child of the root, adapt the navigation:

```csharp
// Under a "Settings" node:
siteRoot?.FirstChild("Settings")?.FirstChild(ErrorPageAlias)
// Deep search (slower, last resort):
siteRoot?.Descendants().FirstOrDefault(x => x.ContentType.Alias == ErrorPageAlias)
```

## Testing & troubleshooting

- Navigate to a non-existent URL → the custom page should display with a 404 status.
- Not showing? Check for competing ContentFinders/redirect packages, confirm the node is
  published in the agreed location, and that the Document Type alias matches the code exactly.
- Wrong page on multi-site? Each site needs its own error page node as a direct child of its
  root, and domains configured on each root node.

## 500 errors

Handled separately — see the shared [500 controller](500-error-controller.md).
