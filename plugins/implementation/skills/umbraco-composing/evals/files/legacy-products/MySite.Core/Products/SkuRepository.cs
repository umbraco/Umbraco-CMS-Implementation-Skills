using Umbraco.Cms.Infrastructure.Scoping;

namespace MySite.Core.Products;

public class SkuRepository : ISkuRepository
{
    private readonly IScopeProvider _scopeProvider;

    public SkuRepository(IScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

    public async Task<IReadOnlyDictionary<string, Guid>> LoadAllAsync(CancellationToken cancellationToken)
    {
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);
        List<SkuRow> rows = await scope.Database.FetchAsync<SkuRow>("SELECT Sku, ProductKey FROM legacySku");
        return rows.ToDictionary(r => r.Sku, r => r.ProductKey, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class SkuRow
    {
        public string Sku { get; set; } = string.Empty;

        public Guid ProductKey { get; set; }
    }
}
