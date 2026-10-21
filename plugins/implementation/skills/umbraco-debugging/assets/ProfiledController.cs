// Install as Controllers/ProfiledController.cs in the web project.
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Services.Navigation;

namespace <Namespace>.Controllers;

// Shows IProfiler steps, nested, around your own work.
// With Umbraco:CMS:Hosting:Debug = true (set before startup), request
//   /debugging-example/profiled?umbDebug=true
// and each Step appears as a row in that request's MiniProfiler timing tree; a Step inside
// another Step appears as its child. With debug mode off at startup, IProfiler is a no-op and
// the steps are effectively free, so they can stay in the code.
//
// The route is only an example: rename it, or move the pattern into your own controller.
[Route("debugging-example/profiled")]
public class ProfiledController : ControllerBase
{
    private readonly IProfiler _profiler;
    private readonly IDocumentNavigationQueryService _navigation;

    public ProfiledController(IProfiler profiler, IDocumentNavigationQueryService navigation)
    {
        _profiler = profiler;
        _navigation = navigation;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var rootCount = 0;
        var descendantCount = 0;

        // Step(string) returns IDisposable? — `using` on null is safe.
        using (_profiler.Step("ProfiledController: count content"))
        {
            IEnumerable<Guid> rootKeys;
            using (_profiler.Step("Get root keys"))
            {
                if (!_navigation.TryGetRootKeys(out rootKeys))
                {
                    rootKeys = [];
                }
            }

            using (_profiler.Step("Count descendants"))
            {
                foreach (Guid rootKey in rootKeys)
                {
                    rootCount++;
                    if (_navigation.TryGetDescendantsKeys(rootKey, out IEnumerable<Guid> descendantKeys))
                    {
                        descendantCount += descendantKeys.Count();
                    }
                }
            }
        }

        return Content($"Roots: {rootCount}, descendants: {descendantCount}", "text/plain");
    }
}
