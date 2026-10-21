using Umbraco.Cms.Core.Models.PublishedContent;

namespace <Namespace>.Services;

// Key-page lookups in one place. Add methods per page (news section, contact…).
public interface ISiteService
{
    IPublishedContent? GetSiteRoot();
}
