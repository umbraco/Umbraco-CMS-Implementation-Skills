using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Acme.Web.Components;

/// <summary>
/// "Latest news" box on the homepage: the five most recent newsArticle pages under the News node.
/// View: Views/Shared/Components/LatestNews/Default.cshtml
/// </summary>
public class LatestNewsViewComponent : ViewComponent
{
    // Building the list walks the whole news section, so we keep it after the first request.
    private static List<LatestNewsItem>? _latest;

    public IViewComponentResult Invoke(IPublishedContent home)
    {
        if (_latest is null)
        {
            IPublishedContent? newsRoot = home.Children().FirstOrDefault(x => x.ContentType.Alias == "newsLanding");

            _latest = (newsRoot?.Children() ?? [])
                .Where(x => x.ContentType.Alias == "newsArticle" && x.IsVisible())
                .OrderByDescending(x => x.Value<DateTime>("publishDate"))
                .Take(5)
                .Select(x => new LatestNewsItem(x.Value<string>("headline") ?? x.Name, x.Url()))
                .ToList();
        }

        return View(_latest);
    }
}

public record LatestNewsItem(string Headline, string Url);
