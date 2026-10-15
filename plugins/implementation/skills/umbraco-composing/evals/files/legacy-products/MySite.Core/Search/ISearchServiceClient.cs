namespace MySite.Core.Search;

/// <summary>Pushes documents to the external search service over HTTP.</summary>
public interface ISearchServiceClient
{
    Task PushProductAsync(Guid productKey, string name, string url, CancellationToken cancellationToken);
}
