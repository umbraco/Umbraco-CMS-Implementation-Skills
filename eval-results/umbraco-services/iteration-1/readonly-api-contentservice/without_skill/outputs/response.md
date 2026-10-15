# Short answer: not quite. Use the published content cache, not `IContentService`

The endpoint part of your plan is fine. The problem is reading the data with `IContentService.GetPagedChildren`. It is the wrong tool for a public, read-only endpoint, for three reasons.

## Why `IContentService` is the wrong fit here

1. **It queries the database on every call.** `IContentService` is the management/persistence API. The backoffice uses it to create, edit, save and publish content. Each request to `/api/products` would go to the database (with paging) and skip Umbraco's published content cache, which is built for exactly this kind of front-end read.
2. **It returns drafts as well as published content.** `GetPagedChildren` returns `IContent` items whether they are published, unpublished or have pending changes. You would have to filter on `.Published` yourself. On top of that, `content.GetValue<decimal>("price")` returns the **draft** value by default. If an editor has saved an unpublished price change, your public API would leak it unless you call `GetValue("price", published: true)`. That is easy to get wrong.
3. **It gives you raw stored values, not converted ones.** `IContent` values bypass property value converters. `IPublishedContent.Value<T>()` gives you the converted, typed value, and you can use ModelsBuilder models such as `product.Price`.

Using `IContentService` for this is a well-known Umbraco anti-pattern. Keep it for code that **writes** content (imports, migrations, notification handlers that change content).

## What to use instead

Read from the **published content cache** through `IPublishedContent`. In Umbraco 17 a simple option inside a controller is `IPublishedContentQuery` (scoped, which is fine for a controller). Another option is `IUmbracoContextAccessor` → `umbracoContext.Content`.

```csharp
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace MySite.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsApiController : ControllerBase
{
    // Key (GUID) of the "Products" folder node. Better: put it in config / appsettings.
    private static readonly Guid ProductsFolderKey = Guid.Parse("00000000-0000-0000-0000-000000000000");

    private readonly IPublishedContentQuery _publishedContentQuery;

    public ProductsApiController(IPublishedContentQuery publishedContentQuery)
        => _publishedContentQuery = publishedContentQuery;

    [HttpGet]
    public ActionResult<IEnumerable<ProductDto>> Get()
    {
        IPublishedContent? folder = _publishedContentQuery.Content(ProductsFolderKey);
        if (folder is null)
        {
            return NotFound();
        }

        var products = folder
            .Children()                                   // published children only
            .Where(x => x.ContentType.Alias == "product") // or .Children<Product>() with ModelsBuilder
            .Select(x => new ProductDto(
                x.Name,
                x.Value<decimal?>("price")));

        return Ok(products);
    }
}

public record ProductDto(string Name, decimal? Price);
```

With ModelsBuilder you can make it strongly typed:
`folder.Children<Product>().Select(p => new ProductDto(p.Name, p.Price))`.

Notes:

- **Use a plain ASP.NET Core controller** (`ControllerBase` + `[ApiController]` + attribute routing). The old `UmbracoApiController` base class is obsolete in modern Umbraco (14+), so don't build on it.
- **Only published content comes back.** The published cache only holds published content, so you don't need a `.Published` filter, and values are the published ones.
- **Look the folder up by key (GUID), not by integer ID.** Keys stay the same across environments (Umbraco Deploy / uSync); integer IDs don't.
- **Products in sub-folders:** `Children()` only returns direct children. If products can be nested, use `folder.Descendants()` (or `DescendantsOfType("product")`) and keep an eye on performance for very large trees.
- **Large catalogues:** the cache is fast, but if you have thousands of products, consider adding paging (`skip`/`take` query parameters) or output caching (`[OutputCache]` with Umbraco cache-refresher eviction) rather than returning everything.
- **Culture:** on a multilingual site, pass a culture (`x.Name(culture)`, `x.Value<decimal?>("price", culture)`) or set it from a query parameter or header.

## Also consider: the Content Delivery API (possibly no code at all)

Umbraco 17 includes the **Content Delivery API**. Once it is enabled in `appsettings.json` (`Umbraco:CMS:DeliveryApi:Enabled: true`), you can get published products with:

```
GET /umbraco/delivery/api/v2/content?fetch=children:/products/&filter=contentType:product&fields=properties[price]
```

It already handles published-only content, paging, culture and preview, and it is served from the cache. If you don't need the exact `/api/products` URL or a custom response shape, this may be all you need. A custom controller is still a reasonable choice if you want a tiny, purpose-built contract.

## Summary

| | `IContentService.GetPagedChildren` | Published cache (`IPublishedContent`) |
|---|---|---|
| Data source | Database, every request | In-memory/hybrid cache |
| Published only? | No. Includes drafts and unpublished items | Yes |
| Property values | Draft by default, raw stored values | Published, converted, typed |
| Intended use | Writing/managing content | Rendering/reading on the front end |

**Verdict:** keep the controller, but swap `IContentService` for the published content cache (or use the Delivery API).

*Unverified points to check against your exact 17.x version:* the precise `IPublishedContentQuery.Content(Guid)` overload, and the `Children()` extension signatures. Navigation extension methods changed in v15+ with the hybrid cache, and some overloads now take services or a culture. Check against the Umbraco docs ("Querying & Traversal", "Content Delivery API").
