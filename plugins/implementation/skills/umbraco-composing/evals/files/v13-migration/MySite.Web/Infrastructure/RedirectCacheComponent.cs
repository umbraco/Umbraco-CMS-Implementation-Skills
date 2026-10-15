using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;

namespace Acme.Site.Infrastructure;

public class RedirectCacheComposer : ComponentComposer<RedirectCacheComponent> { }

public class RedirectCacheComponent : IComponent
{
    private readonly IRedirectCache _cache;
    private readonly IRuntimeState _runtimeState;

    public RedirectCacheComponent(IRedirectCache cache, IRuntimeState runtimeState)
    {
        _cache = cache;
        _runtimeState = runtimeState;
    }

    public void Initialize()
    {
        if (_runtimeState.Level != RuntimeLevel.Run) return;
        _cache.LoadAllAsync().GetAwaiter().GetResult();
    }

    public void Terminate() => _cache.Flush();
}
