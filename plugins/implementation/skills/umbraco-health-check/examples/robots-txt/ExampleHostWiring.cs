using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.HealthChecks;

namespace Umbraco.Skills.Examples.HealthCheck;

public sealed class ExampleHealthCheckComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddTransient<ExampleHealthCheckController>();
    }
}

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ExampleHealthCheckController(
    HealthCheckCollection checks,
    IHostEnvironment hostEnvironment)
    : Controller
{
    [HttpGet]
    [Route("example/health-check/robots")]
    public async Task<IActionResult> Robots()
    {
        Umbraco.Cms.Core.HealthChecks.HealthCheck check =
            checks.Single(x => x.Id == Guid.Parse("A7D3E9F1-60B4-4C8A-B2D5-9E1F73C6428B"));
        HealthCheckStatus status = (await check.GetStatusAsync()).Single();

        return Content(
            $"{status.ResultType}|{System.IO.File.Exists(Path.Combine(hostEnvironment.ContentRootPath, "robots.txt"))}");
    }

    [Authorize]
    [HttpPost]
    [Route("example/health-check/robots")]
    public IActionResult ExecuteRobotsAction([FromQuery(Name = "action")] string? requestedAction = null)
    {
        if (string.IsNullOrWhiteSpace(requestedAction))
        {
            return BadRequest("An action alias is required.");
        }

        Umbraco.Cms.Core.HealthChecks.HealthCheck check =
            checks.Single(x => x.Id == Guid.Parse("A7D3E9F1-60B4-4C8A-B2D5-9E1F73C6428B"));

        try
        {
            HealthCheckStatus status = check.ExecuteAction(new HealthCheckAction(requestedAction, check.Id));
            return Content(
                $"{status.ResultType}|{System.IO.File.Exists(Path.Combine(hostEnvironment.ContentRootPath, "robots.txt"))}");
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }

}
