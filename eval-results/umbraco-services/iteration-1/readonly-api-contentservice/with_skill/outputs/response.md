# Short answer: no. Read from the published cache, not `IContentService`

The endpoint is a good idea, but `IContentService` is the wrong service for it. In Umbraco, **management services are for changing data, and the published cache is for reading it.** The Umbraco 17 docs say:

> "Management Services provide access to allow the modification of Umbraco data. The services are not optimized for displaying data."
> — [Services and Helpers (v17)](https://docs.umbraco.com/umbraco-cms/17.latest/extend-your-project/server-side-extensions/services.md)

## What goes wrong with `IContentService.GetPagedChildren`

1. **Every request hits the database.** `IContentService` takes a read lock and queries the DB on every call. A public `GET /api/products` would add a SQL round-trip (plus property data loading) to each hit. The published cache is in memory.
2. **It returns `IContent`, not published content.** You get every child: drafts, unpublished pages and pages scheduled for later. You'd have to filter on `Published` yourself.
3. **Property values default to the draft.** `IContent.GetValue("price")` returns the *edited* value. If an editor has saved a new price but not published it, your API would show the unpublished price unless you remember `GetValue<decimal>("price", published: true)`.
4. **Paging gets in the way.** `GetPagedChildren(id, pageIndex, pageSize, out total)` makes you loop through pages to get "every product". The cache has no such limit.

## Do this instead

Inject `IPublishedContentQuery` into the controller. It is **scoped** (one per request), which suits a controller since controllers are created per request. Use `IDocumentNavigationQueryService` to get the folder's children: it keeps the tree structure in memory, so no DB query. The cache only returns **published** content, so drafts and unpublished pages are excluded automatically, and values come from the published version.

```csharp
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Extensions;

namespace MySite.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsApiController : ControllerBase
{
    // Key of the "Products" folder node (Info tab in the backoffice).
    // Better: read it from appsettings rather than hard-coding it.
    private static readonly Guid ProductsFolderKey = Guid.Parse("00000000-0000-0000-0000-000000000000");

    private readonly IPublishedContentQuery _publishedContentQuery;
    private readonly IDocumentNavigationQueryService _navigation;

    public ProductsApiController(
        IPublishedContentQuery publishedContentQuery,
        IDocumentNavigationQueryService navigation)
    {
        _publishedContentQuery = publishedContentQuery;
        _navigation = navigation;
    }

    [HttpGet]
    public ActionResult<IEnumerable<ProductDto>> Get()
    {
        // Child keys of the given document type, straight from the in-memory navigation structure.
        if (!_navigation.TryGetChildrenKeysOfType(ProductsFolderKey, "product", out IEnumerable<Guid> keys))
        {
            return NotFound();
        }

        var products = keys
            .Select(key => _publishedContentQuery.Content(key)) // null if not published
            .OfType<IPublishedContent>()
            .Select(p => new ProductDto(p.Name, p.Value<decimal>("price")))
            .ToList();

        return Ok(products);
    }
}

public record ProductDto(string Name, decimal Price);
```

You don't need a composer: ASP.NET Core picks up the controller, and both injected services are already registered by Umbraco.

Before you use it, change these to match your site:

- `"product"` and `"price"` are placeholder aliases. Use your actual Document Type alias and property alias. If you use ModelsBuilder, you can write `.OfType<Product>()` and `p.Price` instead of the string alias.
- If your price is a text or Decimal property with a different underlying type, adjust `Value<decimal>`.
- **Multilingual site?** `Name` and `Value` follow the request's culture. Pass a `culture:` argument if the API must return a specific language.
- **Prefer the typed model or a ModelsBuilder strongly-typed `Children()`** if it suits your codebase. The important part is that the read goes through the cache.

## Rules of thumb

| Doing | Use |
|---|---|
| Reading published content (APIs, views, controllers) | `IPublishedContentQuery`, `UmbracoHelper`, navigation services |
| Creating, editing, publishing or deleting content | `IContentService` (and `IMediaService`, `IMemberService`, ...) |
| Reading content with no HTTP request (background job, hosted service) | Singleton + `IUmbracoContextFactory.EnsureUmbracoContext()` in a `using`. `IPublishedContentQuery` relies on `HttpContext` there and won't work |

## Worth considering: no custom code at all

If you only need name and price for consumers outside the site, Umbraco 17's built-in **Content Delivery API** can already return published content filtered by document type (for example `filter=contentType:product`) once it's enabled in `appsettings.json`. A custom endpoint makes sense when you want a minimal fixed shape like `{ name, price }`, or don't want to enable the Delivery API. Both read from the published cache.

## Caveats

- **Not compiled or run.** No project was provided. The pattern (scoped `IPublishedContentQuery` in a controller, navigation service for structure) is the one this skill's verified assets use on Umbraco 17.5.3. The exact endpoint above, including the `TryGetChildrenKeysOfType` overload, was not compiled. Build it and request `/api/products` once to confirm.
- I assumed Umbraco 17 because you said so. Check the `Umbraco.Cms` version in your `.csproj` if in doubt.
