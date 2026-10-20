using Umbraco.Cms.Core.HealthChecks;

namespace Acme.Umbraco.Media;

[HealthCheck("6c1f3a9e-2b84-4d07-9e5a-7f0b3c2d1e48", "Acme media CDN", Group = "Acme")]
public class AcmeHealthCheck : HealthCheck
{
    public override Task<IEnumerable<HealthCheckStatus>> GetStatusAsync()
        => Task.FromResult<IEnumerable<HealthCheckStatus>>(
            [new HealthCheckStatus("Acme media CDN is reachable.") { ResultType = StatusResultType.Success }]);

    public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
        => throw new InvalidOperationException("This check has no actions.");
}
