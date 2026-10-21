using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Acme.Web.Controllers;

/// <summary>
/// Imports products from the PIM nightly export. POST api/products/import with a JSON array.
/// Products are created under the "Products" node and saved (not published) for editors to review.
/// </summary>
[ApiController]
[Route("api/products/import")]
public class ProductImportController : ControllerBase
{
    private static readonly Guid ProductsRootKey = new("4b2f0c7e-91d3-4a6e-8f15-c3e7a20b9d48");

    private readonly IContentService _contentService;
    private readonly ILogger<ProductImportController> _logger;

    public ProductImportController(IContentService contentService, ILogger<ProductImportController> logger)
    {
        _contentService = contentService;
        _logger = logger;
    }

    [HttpPost]
    public IActionResult Import([FromBody] ProductImportRow[] rows)
    {
        var saved = 0;
        var failed = new List<string>();

        foreach (ProductImportRow row in rows)
        {
            IContent product = _contentService.Create(row.Name, ProductsRootKey, "product");
            product.SetValue("sku", row.Sku);
            product.SetValue("price", row.Price);

            // This reports success for every row, but some products never show up in the backoffice.
            // We want to step into Save to see what Umbraco does with them.
            var result = _contentService.Save(product);

            if (result.Success)
            {
                saved++;
            }
            else
            {
                failed.Add(row.Sku);
                _logger.LogWarning("Product {Sku} was not saved: {Result}", row.Sku, result.Result);
            }
        }

        return Ok(new { saved, failed });
    }
}

public record ProductImportRow(string Name, string Sku, decimal Price);
