---
name: umbraco-custom-error-pages
description: >
  Implement custom 404 and 500 error pages in Umbraco CMS 13-17+.
  Trigger: user asks to "add custom error pages", "set up 404 page", "handle 500 errors",
  "create page not found", "implement error handling", "customize error pages in Umbraco".
  SKIP: non-Umbraco projects.
---

# Custom Error Pages

Implement custom 404 and 500 error pages in Umbraco 13-17+. Error pages are Umbraco content nodes located via structural discovery (`Root -> FirstChild(ErrorPageAlias)`).

Supports single-site and multi-site: the 404 finder uses `request.Domain` to resolve the correct root per site. The 500 controller falls back to the first root since domain context is unavailable after an unhandled exception.

---

## Step 1 - Discovery & Validation

Search the project for:
- Root namespace (`.csproj` or any `.cs` file)
- Umbraco version - read the `Umbraco.Cms` package version from `.csproj` or `Directory.Packages.props`
- Existing folder conventions: `Controllers/`, `ContentFinders/`, `Composers/`
- Whether `Startup.cs` exists - determines where middleware is registered (see Step 4)
- Whether Umbraco Deploy/Cloud is in use - check `.csproj` for `Umbraco.Deploy` package reference

IF Umbraco version >= 14:
  -> `IUmbracoContextFactory` still works but generates obsolete warnings
  -> For warning-free code in Umbraco 14+, use `IUmbracoContextAccessor` instead:
    - Inject `IUmbracoContextAccessor` instead of `IUmbracoContextFactory`
    - Use `umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext)` to get the context
    - Access content via `umbracoContext.Content` instead of factory methods
  -> The asset code uses `IUmbracoContextFactory` for broader compatibility; update if user prefers warning-free code

Ask user:
1. **Which error pages?** (404, 500 - can choose one or both)
2. **Where will the error pages live in the content tree?** Use the answer to write the correct `FirstChild()` navigation chain - see Custom Structure Examples in Troubleshooting.

---

## Step 2 - Create Document Types

### Standard (no Umbraco Deploy/Cloud)

Guide the user to create the Document Type(s) manually in the backoffice:

1. Go to **Settings** -> Create **Document Type with Template**
2. Use the following for each selected error page:

**404:**
- Name: `Error Page 404`
- Alias: `ErrorPage404`
- Properties: `heading` (Textstring), `message` (Rich Text Editor)

**500:**
- Name: `Error Page 500`
- Alias: `ErrorPage500`
- Properties: `heading` (Textstring), `message` (Rich Text Editor)

3. Go to **Content** -> create the node in the agreed location -> **Publish**

> **Required:** The content node must exist and be published before error pages will display.
> If missing, the controller logs: `"500 error page node not found. Expected a published content node with Document Type alias '...'"`.

### Umbraco Deploy / Cloud

Create a UDA file for each selected error page under `data/revision/` in the project. The UDA should describe a Document Type with:
- `Alias`: `ErrorPage404` or `ErrorPage500`
- `DefaultTemplate`: same as alias
- Properties: `heading` (Umbraco.TextBox), `message` (Umbraco.RichText)

Deploy will pick up the file on next extraction and create the Document Type automatically. Then create and publish the content node in the backoffice.

---

## Step 3 - Implement 404 (if selected)

Read `assets/PageNotFoundContentFinder.cs`. Replace placeholders and write to project:
- `<Namespace>` -> project namespace
- `<ErrorPageAlias>` -> `ErrorPage404`

**File placement:**
- IF `ContentFinders/` folder exists with `.cs` files -> place the ContentFinder file there
- ELSE IF `Composers/` folder exists -> place the composer class in Composers/, and the ContentFinder in a new `ContentFinders/` folder
- ELSE -> create `ContentFinders/` and write the ContentFinder file there (include composer class in the same file)

**Important:** The file should be placed in the appropriate folder based on existing project structure. If the project already has ContentFinders/, use that. If it has Composers/ but no ContentFinders/, create ContentFinders/ for the finder.

**Multi-site:** The finder uses `request.Domain?.ContentId` to resolve the correct root node per site. For sites without domains configured it falls back to the first root node.

---

## Step 4 - Implement 500 (if selected)

500s are caught by `UseExceptionHandler` middleware before Umbraco's routing pipeline, so `IContentFinder` cannot intercept them - a plain MVC controller is the only viable approach.

The controller uses `IExceptionHandlerPathFeature` to detect a real unhandled exception. Do **not** use `Response.StatusCode` - it may not be set yet at this point in the pipeline.

The plain-text fallback only fires when the Umbraco context is unavailable (e.g. startup failure). In normal operation the content node must exist.

**Multi-site limitation:** Domain context is not available after an unhandled exception. The 500 controller always uses the first root node. For multi-site setups, each site's error page must have the same Document Type alias, placed as a direct child of each root.

Read `assets/ErrorController.cs`. Replace placeholders and write to `Controllers/` folder (create if needed):
- `<Namespace>` -> project namespace
- `<ErrorPageAlias>` -> `ErrorPage500`

### Update appsettings.json
Add `/error/` to reserved paths:
```json
{
  "Umbraco": {
    "CMS": {
      "Global": {
        "ReservedPaths": "~/app_plugins/,~/install/,~/mini-profiler-resources/,~/umbraco/,~/error/"
      }
    }
  }
}
```

### Register middleware

Add `app.UseExceptionHandler("/error");` before `app.UseUmbraco()`.

- **`Startup.cs` exists** -> add it in `Startup.Configure()`
- **No `Startup.cs`** -> add it in `Program.cs`

---

## Step 5 - Testing

**404:** Navigate to non-existent URL -> verify custom page displays

**500:** Add invalid code to template (e.g. `@Model.ValueTest("test")`) -> verify custom page displays -> revert

---

## Troubleshooting

**Error pages not showing:**
- Check for custom ContentFinders, redirect packages, or rewrite rules
- Verify error pages are published and in the agreed location
- Confirm Document Type aliases match code exactly

**500 page showing plain text:**
- Check logs for `"500 error page node not found"` - content node is missing or unpublished
- Confirm `app.UseExceptionHandler("/error")` is before `app.UseUmbraco()`
- Confirm `/error/` is in `ReservedPaths` in `appsettings.json`

**Wrong 404 page shown on multi-site:**
- Verify each site has its own error page node as a direct child of its root
- Verify domains are configured in the Umbraco backoffice under each root node

**Custom Structure Examples:**
```csharp
// Error pages under a "Settings" node (adapt siteRoot navigation):
siteRoot?.FirstChild("Settings")?.FirstChild(ErrorPageAlias)

// Deep search (slower, use sparingly):
siteRoot?.Descendants().FirstOrDefault(x => x.ContentType.Alias == ErrorPageAlias)
```

---

## Done

- ✅ 404 uses `IContentLastChanceFinder` with `SetResponseStatus(404)`
- ✅ 404 finder resolves correct root per site via `request.Domain`
- ✅ 500 uses `IExceptionHandlerPathFeature` to detect real exceptions
- ✅ Missing content node logs a warning
- ✅ File placement follows existing project conventions
- ✅ `FirstChild()` navigation matches the agreed content tree structure
- ✅ Check for conflicts with custom routing/redirects
- ✅ For Umbraco 14+, provide option to use `IUmbracoContextAccessor` for warning-free code
