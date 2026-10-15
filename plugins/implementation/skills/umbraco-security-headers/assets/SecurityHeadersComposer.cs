using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace <Namespace>;

/// <summary>
/// Registers <see cref="SecurityHeadersMiddleware"/> at the start of Umbraco's pipeline, so it
/// runs before static files, media and the backoffice. Program.cs is not edited.
/// </summary>
public class SecurityHeadersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<SecurityHeadersSettings>(
            builder.Config.GetSection(SecurityHeadersSettings.SectionName));

        builder.Services.AddSingleton<SecurityHeadersMiddleware>();

        // PrePipeline, not PostPipeline: static files and media short-circuit the pipeline, so
        // anything registered after them never sees those responses.
        builder.Services.Configure<UmbracoPipelineOptions>(options =>
            options.AddFilter(new UmbracoPipelineFilter(nameof(SecurityHeadersComposer))
            {
                PrePipeline = app => app.UseMiddleware<SecurityHeadersMiddleware>(),
            }));
    }
}
