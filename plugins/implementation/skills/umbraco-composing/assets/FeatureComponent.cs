using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;

namespace <Namespace>;

// Startup/shutdown work for <Feature>. RuntimeAsyncComponentBase skips both methods unless the
// runtime level is Run and Umbraco isn't restarting, so this never runs during install or upgrade.
public sealed class <Feature>Component : RuntimeAsyncComponentBase
{
    private readonly ILogger<<Feature>Component> _logger;

    public <Feature>Component(IRuntimeState runtimeState, ILogger<<Feature>Component> logger)
        : base(runtimeState)
        => _logger = logger;

    protected override Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Startup work goes here. Keep it fast: boot waits for every component in turn.
        _logger.LogInformation("<Feature> initialised");
        return Task.CompletedTask;
    }

    // Optional: remove if there is nothing to clean up. Components terminate in reverse order.
    protected override Task TerminateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
