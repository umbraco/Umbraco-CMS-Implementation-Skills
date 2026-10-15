using System.Globalization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Web.HealthCheck.Checks.SEO;

[HealthCheck("A7D3E9F1-60B4-4C8A-B2D5-9E1F73C6428B", "Robots.txt",
    Description = "Create a robots.txt file to provide crawler guidance for the site.",
    Group = "SEO")]
public class RobotsTxtHealthCheck : Umbraco.Cms.Core.HealthChecks.HealthCheck
{
    private const string AddDefaultRobotsTxtAction = "addDefaultRobotsTxtFile";
    private const string DefaultRobotsTxtContent = """
        # robots.txt for Umbraco
        User-agent: *
        Disallow: /umbraco/
        """;
    private readonly IHostEnvironment hostEnvironment;
    private readonly ILogger<RobotsTxtHealthCheck> logger;
    private readonly ILocalizedTextService textService;

    public RobotsTxtHealthCheck(
        ILocalizedTextService textService,
        IHostEnvironment hostEnvironment,
        ILogger<RobotsTxtHealthCheck> logger)
    {
        this.textService = textService;
        this.hostEnvironment = hostEnvironment;
        this.logger = logger;
    }

    public override Task<IEnumerable<HealthCheckStatus>> GetStatusAsync() =>
        Task.FromResult<IEnumerable<HealthCheckStatus>>(new[] { CheckForRobotsTxtFile() });

    public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
    {
        if (action.HealthCheckId != Id)
        {
            throw new InvalidOperationException(
                $"Action '{action.Alias}' targets health check '{action.HealthCheckId}', not '{Id}'.");
        }

        return action.Alias switch
        {
            AddDefaultRobotsTxtAction => AddDefaultRobotsTxtFile(),
            _ => throw new InvalidOperationException($"Action '{action.Alias}' is not supported.")
        };
    }

    private HealthCheckStatus CheckForRobotsTxtFile()
    {
        string robotsTxtPath = GetRobotsTxtPath();
        var exists = File.Exists(robotsTxtPath);
        var message = exists
            ? textService.Localize("healthcheck", "seoRobotsCheckSuccess", CultureInfo.CurrentUICulture)
            : textService.Localize("healthcheck", "seoRobotsCheckFailed", CultureInfo.CurrentUICulture);

        var actions = new List<HealthCheckAction>();
        if (!exists)
        {
            actions.Add(new HealthCheckAction(AddDefaultRobotsTxtAction, Id)
            {
                Name = textService.Localize("healthcheck", "seoRobotsRectifyButtonName", CultureInfo.CurrentUICulture),
                Description = textService.Localize("healthcheck", "seoRobotsRectifyDescription", CultureInfo.CurrentUICulture)
            });
        }

        return new HealthCheckStatus(message)
        {
            ResultType = exists ? StatusResultType.Success : StatusResultType.Error,
            Actions = actions
        };
    }

    private HealthCheckStatus AddDefaultRobotsTxtFile()
    {
        string robotsTxtPath = GetRobotsTxtPath();

        if (File.Exists(robotsTxtPath))
        {
            return SuccessStatus();
        }

        string temporaryPath = $"{robotsTxtPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 4096,
                       options: FileOptions.SequentialScan))
            {
                using var writer = new StreamWriter(stream);
                writer.Write(DefaultRobotsTxtContent);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, robotsTxtPath, overwrite: false);
            return SuccessStatus();
        }
        catch (IOException exception)
        {
            if (File.Exists(robotsTxtPath))
            {
                return SuccessStatus();
            }

            logger.LogError(exception, "Could not write robots.txt to the root of the site.");
            return WriteFailureStatus();
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogError(exception, "Could not write robots.txt to the root of the site.");
            return WriteFailureStatus();
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private string GetRobotsTxtPath()
    {
        string contentRootPath = Path.GetFullPath(hostEnvironment.ContentRootPath);
        string robotsTxtPath = Path.GetFullPath(Path.Combine(contentRootPath, "robots.txt"));
        string contentRootPrefix = contentRootPath.EndsWith(Path.DirectorySeparatorChar)
            ? contentRootPath
            : contentRootPath + Path.DirectorySeparatorChar;

        if (!robotsTxtPath.StartsWith(contentRootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The robots.txt path is outside the site content root.");
        }

        return robotsTxtPath;
    }

    private HealthCheckStatus SuccessStatus() =>
        new(textService.Localize("healthcheck", "seoRobotsCheckSuccess", CultureInfo.CurrentUICulture))
        {
            ResultType = StatusResultType.Success,
            Actions = new List<HealthCheckAction>()
        };

    private HealthCheckStatus WriteFailureStatus() =>
        new(textService.Localize("healthcheck", "seoRobotsRectifyFailed", CultureInfo.CurrentUICulture))
        {
            ResultType = StatusResultType.Error,
            Actions = new List<HealthCheckAction>()
        };

}
