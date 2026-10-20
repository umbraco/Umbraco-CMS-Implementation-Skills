namespace MySite.Core.Products;

/// <summary>Reads the legacy SKU → product key table from the Umbraco database.</summary>
public interface ISkuRepository
{
    Task<IReadOnlyDictionary<string, Guid>> LoadAllAsync(CancellationToken cancellationToken);
}
