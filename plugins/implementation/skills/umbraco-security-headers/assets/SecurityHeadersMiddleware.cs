using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Security;

namespace <Namespace>;

/// <summary>
/// Sends one Content Security Policy per surface (backoffice under /umbraco, website everywhere
/// else) plus the other security headers Umbraco's health checks look for.
/// </summary>
/// <remarks>
/// Headers are assigned, never appended, so the app never sends two of the same header.
/// </remarks>
public class SecurityHeadersMiddleware : IMiddleware
{
    private readonly ICspNonceService _cspNonceService;
    private readonly IOptionsMonitor<SecurityHeadersSettings> _settings;

    public SecurityHeadersMiddleware(
        ICspNonceService cspNonceService,
        IOptionsMonitor<SecurityHeadersSettings> settings)
    {
        _cspNonceService = cspNonceService;
        _settings = settings;
    }

    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Umbraco generates one nonce per request and tags its backoffice import map with it;
        // the asp-csp-nonce tag helper puts the same value on your own inline tags.
        var nonce = _cspNonceService.GetNonce();

        // Set now, so Umbraco's website output cache stores the CSP with the page it caches.
        ApplyHeaders(context, nonce);

        // And again as the response starts, because an exception handler clears the headers
        // before rendering the error page.
        context.Response.OnStarting(() =>
        {
            ApplyHeaders(context, nonce);
            return Task.CompletedTask;
        });

        return next(context);
    }

    private void ApplyHeaders(HttpContext context, string? nonce)
    {
        IHeaderDictionary headers = context.Response.Headers;

        var policy = context.Request.Path.StartsWithSegments("/umbraco")
            ? BackOfficePolicy(nonce)
            : SitePolicy(nonce);

        var cspHeader = _settings.CurrentValue.ReportOnly
            ? "Content-Security-Policy-Report-Only"
            : "Content-Security-Policy";

        // On an output-cache hit the cache has replayed the CSP the page was rendered with, and
        // the cached HTML carries that CSP's nonce. Replacing it with this request's nonce would
        // block every nonce-tagged script on the cached page.
        if (!headers.ContainsKey(cspHeader))
        {
            headers[cspHeader] = policy;
        }

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "SAMEORIGIN";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        // Deprecated and flagged by Umbraco's health check; CSP replaces it.
        headers.Remove("X-XSS-Protection");
    }

    // The sources the Umbraco backoffice needs (from Umbraco's CSP health check guide).
    // Kept separate so the website never inherits 'unsafe-inline' styles or the Umbraco hosts.
    // No form-action: it would block the redirect to an external login provider.
    private static string BackOfficePolicy(string? nonce) => string.Join("; ",
        "default-src 'self'",
        $"script-src 'self' 'nonce-{nonce}'",
        "style-src 'self' 'unsafe-inline'",
        "img-src 'self' blob: data: https://news-dashboard.umbraco.com",
        "connect-src 'self'",
        "font-src 'self'",
        "frame-src 'self' https://marketplace.umbraco.com",
        "frame-ancestors 'self'",
        "object-src 'none'",
        "base-uri 'self'");

    // EDIT for your front end: add the origins your site loads from (analytics, fonts, CDN).
    // Keep 'unsafe-inline' out of script-src: tag your own inline scripts with asp-csp-nonce.
    // The nonce in style-src covers <style asp-csp-nonce>. Browsers ignore 'unsafe-inline' in a
    // directive that has a nonce, so if style="…" attributes force 'unsafe-inline' into
    // style-src, take the style-src nonce out.
    // Output-cached pages reuse one nonce for every visitor until the cache entry expires, so
    // prefer a hash or an external file for inline scripts on those pages.
    private static string SitePolicy(string? nonce) => string.Join("; ",
        "default-src 'self'",
        $"script-src 'self' 'nonce-{nonce}'",
        $"style-src 'self' 'nonce-{nonce}'",
        "img-src 'self' data:",
        "connect-src 'self'",
        "font-src 'self'",
        "frame-src 'self'",
        "frame-ancestors 'self'", // Backoffice preview frames the site; widen if the backoffice is on another origin.
        "object-src 'none'",
        "base-uri 'self'",
        "form-action 'self'"); // Add the provider's origin if members sign in externally.
}
