# Using the Services layer in Razor views

**Symptoms:** slow page rendering, database load that scales with traffic, unexpected content
changes.
**Look for:** `@inject IContentService`, `IMediaService`, `IMemberService`, or any other
`I*Service` from the Services layer in a `.cshtml` file.
**Docs section:** "Using the Services layer in your views"

`IContentService`, `IMediaService`, `IMemberService`, and similar services hit the **database
directly**. In a Razor view they bypass the published content cache, slow rendering, and can
cause unintended writes.

**Bad:**
```cshtml
@inject IContentService _contentService
@{ var item = _contentService.GetById(1234); }
```

**Good:**
```cshtml
@{ var item = Umbraco.Content(1234); }
```

Read-only APIs that are safe in views: `UmbracoHelper` (`@Umbraco.*`), `ITagQuery`,
`IMemberManager`.

**Related:** [Not caching expensive lookups](missing-lookup-cache.md).
