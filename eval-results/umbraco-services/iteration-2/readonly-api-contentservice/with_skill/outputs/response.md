# Short answer: no, don't use `IContentService` for this

The endpoint idea is fine, but the service is wrong for the job. Umbraco gives you two kinds of service:

| Doing | Use | Avoid |
|---|---|---|
| **Read published content** | The published cache: `IPublishedContentQuery`, plus `IDocumentNavigationQueryService` for children/structure | `IContentService.GetById`, `GetPagedChildren`, etc. |
| **Change** content (save, publish, delete) | `IContentService` (management service) | Calling it on every request |

Why `IContentService` doesn't fit a public read-only endpoint:

1. **It hits the database on every call.** Each call takes a read lock and runs a DB query (in 17 and 18). `GetPagedChildren` on a public `GET` means a DB round-trip on every request, while the published cache already has the same data in memory.
2. **It returns the wrong data.** `IContentService` returns `IContent`, which is the *draft/management* model. You get unpublished, draft and scheduled items too, and `GetValue("price")` reads the **latest saved draft**, not the published value. You'd have to filter on `Published` yourself and could still leak an edited-but-unpublished price.
3. **You'd have to page it yourself.** `GetPagedChildren(id, pageIndex, pageSize, out total)` forces a paging loop to get "every product".

The published cache gives you exactly "every **published** product, with its **published** values". That's what this endpoint needs.

## Recommended shape

Inject `IPublishedContentQuery` (reads published content) and `IDocumentNavigationQueryService` (in-memory tree structure: children keys) into the controller with constructor injection. No custom registration is needed because both are built-in.

```csharp
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core;                 // IPublishedContentQuery
using Umbraco.Extensions;               // .Value<T>()

namespace MySite.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsApiController : ControllerBase
{
    // Key of the "Products" folder node. Better: read it from appsettings (see notes).
    private static readonly Guid ProductsFolderKey = Guid.Parse("00000000-0000-0000-0000-000000000000");

    private readonly IPublishedContentQuery _contentQuery;
    private readonly IDocumentNavigationQueryService _navigation;

    public ProductsApiController(
        IPublishedContentQuery contentQuery,
        IDocumentNavigationQueryService navigation)
    {
        _contentQuery = contentQuery;
        _navigation = navigation;
    }

    [HttpGet]
    public ActionResult<IEnumerable<ProductDto>> Get()
    {
        // Child keys from the in-memory navigation structure (no DB hit).
        // Use "product" = your product document type alias.
        if (!_navigation.TryGetChildrenKeysOfType(ProductsFolderKey, "product", out IEnumerable<Guid> keys))
        {
            return NotFound();
        }

        // The navigation tree includes unpublished nodes; the published cache returns
        // null for those, so filtering out nulls = "published only".
        var products = keys
            .Select(key => _contentQuery.Content(key))
            .OfType<IPublishedContent>()
            .Select(p => new ProductDto(p.Name, p.Value<decimal>("price")))
            .ToList();

        return Ok(products);
    }

    public record ProductDto(string Name, decimal Price);
}
```

Each class, which service, and why:

- **`ProductsApiController`** uses `IPublishedContentQuery` (published cache, read-only, in memory) to load each product and `IDocumentNavigationQueryService` (in-memory navigation structure) to find the folder's children. Neither touches the database per request. Both are built-in and resolved by constructor injection. Controllers are created per request, so injecting the scoped `IPublishedContentQuery` is fine.
- **`IContentService`** isn't used. Keep it for code that *changes* content, such as an import job or a "set price" admin action.

## Notes and things to adjust

- **Products folder key.** Don't hard-code an int ID. Copy the folder's key (GUID) from the backoffice "Info" tab into `appsettings.json` and bind it through `IOptions<T>`. Or, if the folder is a known document type, find it from `_contentQuery.ContentAtRoot()`.
- **Aliases.** `"product"` and `"price"` are placeholders. Use your real document type and property aliases. If you use ModelsBuilder, you can cast to the generated `Product` model and use `p.Price` instead.
- **Price type.** `Value<decimal>` fits a Decimal/Numeric property. Change the type if your editor stores it differently.
- **Multilingual sites.** If products vary by culture, `Name` and `Value` resolve for the current/default culture. Pass `culture:` explicitly (for example from a query string) if the API needs a specific language.
- **Products nested deeper than one level?** Use `TryGetDescendantsKeysOfType` instead of `TryGetChildrenKeysOfType`.
- **Alternative with no code:** if you'd rather not write a controller, Umbraco's built-in **Content Delivery API** (`/umbraco/delivery/api/v2/content?filter=contentType:product`) already serves published content as JSON once enabled. A small custom endpoint like the one above is still reasonable if you want exactly `{ name, price }` at `/api/products`.
- **Version check.** This applies to Umbraco 17 (and 18). Confirm `Umbraco.Cms` 17.x in your `.csproj` / `Directory.Packages.props`.

**Verification status:** I wrote this from the skill's verified guidance (published cache for reads, `IContentService` only for writes, constructor injection in controllers). I didn't compile or run this exact controller against your project, so build it and call `GET /api/products` to confirm. Pay particular attention to the `TryGetChildrenKeysOfType` signature and your aliases.
