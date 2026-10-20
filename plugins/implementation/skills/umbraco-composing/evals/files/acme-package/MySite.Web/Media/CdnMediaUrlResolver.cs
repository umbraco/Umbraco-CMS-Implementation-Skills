using Acme.Umbraco.Media;

namespace MySite.Web.Media;

/// <summary>Builds CDN media URLs. Reads the request's market, so it must be scoped.</summary>
public class CdnMediaUrlResolver : IAcmeMediaUrlResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CdnMediaUrlResolver(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public string Resolve(string mediaPath, int? width = null, int? height = null)
    {
        string market = _httpContextAccessor.HttpContext?.Request.Headers["X-Market"].ToString() is { Length: > 0 } m
            ? m
            : "global";
        return $"https://cdn.mysite.example/{market}{mediaPath}?w={width}&h={height}";
    }
}
