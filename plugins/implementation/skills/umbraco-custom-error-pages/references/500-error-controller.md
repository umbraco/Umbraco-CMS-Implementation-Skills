# 500 errors — shared controller (single method)

There is **no config-based option for 500s** — unhandled exceptions are caught by ASP.NET
Core's `UseExceptionHandler` middleware *before* Umbraco's routing pipeline, so neither
`Error404Collection` nor an `IContentFinder` can intercept them. A plain MVC controller is the
only viable approach, whichever 404 approach was chosen.

The asset is an improved variant of the docs' sample
([Implement Custom Error Pages, "500 Errors"](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/tutorials/custom-error-page)):
it resolves the page structurally by alias instead of a hardcoded GUID, detects real
exceptions via `IExceptionHandlerPathFeature` (not `Response.StatusCode`, which may not be set
yet), and falls back to plain text if the Umbraco context is unavailable (e.g. startup
failure).

## Steps

1. **Backoffice:** create a **Document Type with Template** named *Error Page 500* (alias
   `ErrorPage500`) with `heading` (Textstring) and `message` (Rich Text), and create +
   **publish** a content node of that type as a direct child of the root. Prefer the
   [Umbraco Developer MCP](https://docs.umbraco.com/umbraco-in-ai/mcp/cms-developer-mcp) to do
   this directly if available; otherwise walk the user through it manually in the backoffice.
2. **Code:** read [`../assets/ErrorController.cs`](../assets/ErrorController.cs), replace
   `<Namespace>` → project namespace and `<ErrorPageAlias>` → `ErrorPage500`, and write it to
   `Controllers/` (create the folder if needed).
3. **appsettings.json:** add `~/error/` to reserved paths:
   ```json
   { "Umbraco": { "CMS": { "Global": {
     "ReservedPaths": "~/app_plugins/,~/install/,~/mini-profiler-resources/,~/umbraco/,~/error/"
   } } } }
   ```
4. **Middleware:** add `app.UseExceptionHandler("/error");` **before** `app.UseUmbraco()` —
   in `Startup.Configure()` if `Startup.cs` exists, otherwise in `Program.cs`.
5. If a real `.csproj` is present, run `dotnet build` and fix any errors; if not, say so and
   state whether the code is expected to compile against the documented APIs used.

## Multi-site limitation

Domain context is unavailable after an unhandled exception, so the controller always uses the
**first root node**. For multi-site setups, each site's 500 page must use the same Document
Type alias, placed as a direct child of each root — but all sites will be served from the
first root's page. Tell the user about this limitation up front.

## Testing & troubleshooting

- Add invalid code to a template (e.g. `@Model.ValueTest("test")`) → verify the custom page
  displays → revert.
- Plain-text response instead of the page? Check logs for `"500 error page node not found"`
  (node missing/unpublished), confirm `UseExceptionHandler` runs before `UseUmbraco()`, and
  that `~/error/` is in `ReservedPaths`.
