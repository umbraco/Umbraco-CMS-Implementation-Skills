namespace Acme.Web.Services;

/// <summary>Talks to the analytics service. Registered in DI by ArticleStatsComposer.</summary>
public interface IArticleStatsClient
{
    /// <summary>Titles of the currently trending articles, most popular first.</summary>
    Task<IReadOnlyList<string>> GetTrendingAsync(CancellationToken cancellationToken);
}
