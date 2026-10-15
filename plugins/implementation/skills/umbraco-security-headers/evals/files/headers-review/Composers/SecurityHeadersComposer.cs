using Contoso.Web.Middleware;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace Contoso.Web.Composers;

public class SecurityHeadersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<SecurityHeadersOptions>(builder.Config.GetSection("SecurityHeaders"));
        builder.Services.AddSingleton<SecurityHeadersMiddleware>();

        builder.Services.Configure<UmbracoPipelineOptions>(options =>
            options.AddFilter(new UmbracoPipelineFilter("SecurityHeaders")
            {
                PostPipeline = app => app.UseMiddleware<SecurityHeadersMiddleware>(),
            }));
    }
}
