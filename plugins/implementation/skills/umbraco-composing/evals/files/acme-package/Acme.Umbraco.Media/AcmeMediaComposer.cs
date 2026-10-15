using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;

namespace Acme.Umbraco.Media;

internal sealed class AcmeMediaComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddUnique<IAcmeMediaUrlResolver, AcmeMediaUrlResolver>(ServiceLifetime.Scoped);
}
