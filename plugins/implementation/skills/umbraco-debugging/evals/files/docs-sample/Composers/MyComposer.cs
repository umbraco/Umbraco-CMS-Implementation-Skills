using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Acme.Web.Composers;

// Pasted from Umbraco's "Debugging with SourceLink" docs page, with the namespace changed.
public class MyComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Components().Append<MyComponent>();
    }
}

public class MyComponent : IComponent
{
    private IContentService _contentService;

    public MyComponent(IContentService contentService)
    {
        _contentService = contentService;
    }

    public void Initialize()
    {
        // Add break point & F11 into me
        var root = _contentService.GetRootContent();

        foreach (var item in root)
        {
            // Add break point & F11 into me
            var udi = item.GetUdi();
            var foo = 5;
        }
    }

    public void Terminate()
    {
    }
}
