namespace NotificationEval;

// Existing application boundaries, shared by both configurations. No live services are called.
public sealed record ProductSnapshot(Guid Key, string Name, string Url);

public interface IPublishedProductReader
{
    // Reads the final published product, or null for non-products/unpublished content.
    Task<ProductSnapshot?> ReadAsync(Guid key, CancellationToken cancellationToken);

    // Returns product descendants, not the branch root itself.
    Task<IReadOnlyList<ProductSnapshot>> ReadBranchAsync(Guid rootKey, CancellationToken cancellationToken);
}

public interface IProductCache
{
    void Invalidate(Guid key, bool includeDescendants);
    void InvalidateAll();
}

public interface IPimClient
{
    Task UpsertAsync(ProductSnapshot product, CancellationToken cancellationToken);
}

public interface IProductImporter
{
    // Uses the ambient Umbraco scope. Throws on failure; does not commit independently.
    Task<int> ImportAsync(string feed, CancellationToken cancellationToken);
}

public interface IFreezeWindow
{
    // Returns a veto reason, or null when this feed may be imported.
    Task<string?> GetReasonAsync(string feed, CancellationToken cancellationToken);
}
