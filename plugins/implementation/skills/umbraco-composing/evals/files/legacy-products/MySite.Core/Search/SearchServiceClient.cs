using System.Net.Http.Json;

namespace MySite.Core.Search;

public class SearchServiceClient : ISearchServiceClient
{
    private readonly HttpClient _httpClient;

    public SearchServiceClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task PushProductAsync(Guid productKey, string name, string url, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "products", new { productKey, name, url }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
