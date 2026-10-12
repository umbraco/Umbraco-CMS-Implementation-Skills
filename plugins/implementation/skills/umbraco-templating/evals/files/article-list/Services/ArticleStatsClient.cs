namespace Acme.Web.Services;

public class ArticleStatsClient : IArticleStatsClient
{
    public async Task<IReadOnlyList<string>> GetTrendingAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken); // stands in for the HTTP call
        return ["Ten tips for faster Umbraco sites", "What's new in Umbraco 17"];
    }
}
