using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Contoso.Web.Middleware;

public class SecurityHeadersOptions
{
    public bool ReportOnly { get; set; }
}

public class SecurityHeadersMiddleware : IMiddleware
{
    public const string NonceItemKey = "csp-nonce";

    private readonly IOptions<SecurityHeadersOptions> _options;

    public SecurityHeadersMiddleware(IOptions<SecurityHeadersOptions> options) => _options = options;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items[NonceItemKey] = nonce;

        var policy =
            "default-src 'self'; " +
            $"script-src 'nonce-{nonce}' 'strict-dynamic' https:; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "img-src 'self' data: blob: https://news-dashboard.umbraco.com; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "frame-src 'self' https://marketplace.umbraco.com; " +
            "object-src 'none'; base-uri 'self'";

        var header = _options.Value.ReportOnly
            ? "Content-Security-Policy-Report-Only"
            : "Content-Security-Policy";

        context.Response.Headers[header] = policy;
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers.Remove("Server");

        await next(context);
    }
}
