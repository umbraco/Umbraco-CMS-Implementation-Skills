using Microsoft.Extensions.Configuration;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.DependencyInjection;

namespace <Namespace>;

/// <summary>
/// Pins this server's role from configuration instead of letting Umbraco elect one at startup.
/// Set <c>Deployment:ServerRole</c> (env var <c>Deployment__ServerRole</c>) to
/// <c>SchedulingPublisher</c> on the single backoffice server and <c>Subscriber</c> on every
/// scalable front-end server.
/// </summary>
public class ConfiguredServerRoleComposer : IComposer
{
    public const string ConfigKey = "Deployment:ServerRole";

    public void Compose(IUmbracoBuilder builder)
    {
        ServerRole? role = ParseRole(builder.Config[ConfigKey]);

        // Unset = keep Umbraco's automatic election (single-server and local development).
        if (role is not null)
        {
            builder.SetServerRegistrar(new ConfiguredServerRoleAccessor(role.Value));
        }
    }

    /// <summary>
    /// Null when unset. Throws on anything else that is not an explicit role, so a typo fails the
    /// boot instead of silently leaving a front-end server eligible to run the scheduled jobs.
    /// </summary>
    public static ServerRole? ParseRole(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return null;
        }

        // Match by NAME, not Enum.TryParse: that also accepts numeric strings, so "3" would
        // silently become SchedulingPublisher.
        foreach (ServerRole explicitRole in new[] { ServerRole.SchedulingPublisher, ServerRole.Subscriber })
        {
            if (string.Equals(configured.Trim(), explicitRole.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return explicitRole;
            }
        }

        throw new InvalidOperationException(
            $"'{ConfigKey}' must be '{nameof(ServerRole.SchedulingPublisher)}' or '{nameof(ServerRole.Subscriber)}', but was '{configured}'.");
    }
}

public class ConfiguredServerRoleAccessor : IServerRoleAccessor
{
    public ConfiguredServerRoleAccessor(ServerRole role) => CurrentServerRole = role;

    public ServerRole CurrentServerRole { get; }
}
