using System.Globalization;
using System.Net;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Cms.Core.Services;
using Umbraco.Web.HealthCheck.Checks.SEO;

namespace Umbraco_CMS.Skills.TestHost.Shared;

[TestFixture]
[NonParallelizable]
public sealed class HealthCheckTests
{
    private string contentRoot = null!;
    private RecordingTextService textService = null!;
    private RecordingLogger<RobotsTxtHealthCheck> logger = null!;

    [SetUp]
    public void SetUp()
    {
        contentRoot = Path.Combine(Path.GetTempPath(), "umbraco-health-check-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(contentRoot);
        textService = new RecordingTextService();
        logger = new RecordingLogger<RobotsTxtHealthCheck>();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(contentRoot))
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    [Test]
    public async Task Missing_file_reports_error_and_exposes_remediation_action()
    {
        // arrange
        RobotsTxtHealthCheck check = CreateCheck();

        // act
        HealthCheckStatus status = (await check.GetStatusAsync()).Single();

        // assert
        Assert.That(status.ResultType, Is.EqualTo(StatusResultType.Error));
        HealthCheckAction action = status.Actions.Single();
        Assert.That(action.Alias, Is.EqualTo("addDefaultRobotsTxtFile"));
        Assert.That(action.HealthCheckId, Is.EqualTo(check.Id));
    }

    [Test]
    public async Task Existing_file_reports_success_without_remediation_action()
    {
        // arrange
        string path = Path.Combine(contentRoot, "robots.txt");
        await File.WriteAllTextAsync(path, "existing content");
        RobotsTxtHealthCheck check = CreateCheck();

        // act
        HealthCheckStatus status = (await check.GetStatusAsync()).Single();

        // assert
        Assert.That(status.ResultType, Is.EqualTo(StatusResultType.Success));
        Assert.That(status.Actions, Is.Empty);
    }

    [Test]
    public void Supported_action_creates_file_and_reports_success()
    {
        // arrange
        RobotsTxtHealthCheck check = CreateCheck();
        HealthCheckAction action = new("addDefaultRobotsTxtFile", check.Id);

        // act
        HealthCheckStatus status = check.ExecuteAction(action);

        // assert
        Assert.That(status.ResultType, Is.EqualTo(StatusResultType.Success));
        Assert.That(File.Exists(Path.Combine(contentRoot, "robots.txt")), Is.True);
        Assert.That(File.ReadAllText(Path.Combine(contentRoot, "robots.txt")), Does.Contain("User-agent: *"));
        Assert.That(File.ReadAllText(Path.Combine(contentRoot, "robots.txt")), Does.Contain("Disallow: /umbraco/"));
    }

    [Test]
    public void Repeated_supported_action_is_idempotent_and_does_not_overwrite_file()
    {
        // arrange
        string path = Path.Combine(contentRoot, "robots.txt");
        File.WriteAllText(path, "content managed by the site");
        RobotsTxtHealthCheck check = CreateCheck();
        HealthCheckAction action = new("addDefaultRobotsTxtFile", check.Id);

        // act
        HealthCheckStatus status = check.ExecuteAction(action);

        // assert
        Assert.That(status.ResultType, Is.EqualTo(StatusResultType.Success));
        Assert.That(File.ReadAllText(path), Is.EqualTo("content managed by the site"));
    }

    [Test]
    public void Unsupported_action_is_rejected()
    {
        // arrange
        RobotsTxtHealthCheck check = CreateCheck();
        HealthCheckAction action = new("unsupported", check.Id);

        // act
        TestDelegate execute = () => check.ExecuteAction(action);

        // assert
        Assert.That(execute, Throws.TypeOf<InvalidOperationException>()
            .With.Message.EqualTo("Action 'unsupported' is not supported."));
    }

    [Test]
    public void Action_for_different_health_check_is_rejected_before_writing()
    {
        // arrange
        RobotsTxtHealthCheck check = CreateCheck();
        HealthCheckAction action = new("addDefaultRobotsTxtFile", Guid.NewGuid());

        // act
        TestDelegate execute = () => check.ExecuteAction(action);

        // assert
        Assert.That(execute, Throws.TypeOf<InvalidOperationException>()
            .With.Message.Contains("targets health check"));
        Assert.That(File.Exists(Path.Combine(contentRoot, "robots.txt")), Is.False);
    }

    [Test]
    public async Task Concurrent_supported_actions_do_not_overwrite_each_other()
    {
        // arrange
        RobotsTxtHealthCheck check = CreateCheck();
        HealthCheckAction action = new("addDefaultRobotsTxtFile", check.Id);

        // act
        HealthCheckStatus[] statuses = await Task.WhenAll(
            Task.Run(() => check.ExecuteAction(action)),
            Task.Run(() => check.ExecuteAction(action)));

        // assert
        Assert.That(statuses, Has.All.Property(nameof(HealthCheckStatus.ResultType))
            .EqualTo(StatusResultType.Success));
        Assert.That(File.ReadAllText(Path.Combine(contentRoot, "robots.txt")),
            Does.Contain("Disallow: /umbraco/"));
        Assert.That(Directory.GetFiles(contentRoot, "*.tmp"), Is.Empty);
    }

    [Test]
    public void Write_failure_reports_error_and_logs_exception()
    {
        // arrange
        string blockedRoot = Path.Combine(contentRoot, "not-a-directory");
        File.WriteAllText(blockedRoot, "a file, not a directory");
        RobotsTxtHealthCheck check = CreateCheck(blockedRoot);
        HealthCheckAction action = new("addDefaultRobotsTxtFile", check.Id);

        // act
        HealthCheckStatus status = check.ExecuteAction(action);

        // assert
        Assert.That(status.ResultType, Is.EqualTo(StatusResultType.Error));
        Assert.That(logger.Entries, Has.Count.EqualTo(1));
        Assert.That(logger.Entries[0].Level, Is.EqualTo(LogLevel.Error));
        Assert.That(logger.Entries[0].Exception, Is.InstanceOf<IOException>());
    }

    [Test]
    public async Task Localization_requests_use_current_ui_culture()
    {
        // arrange
        CultureInfo originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo testCulture = CultureInfo.GetCultureInfo("fr-FR");
        CultureInfo.CurrentUICulture = testCulture;
        try
        {
            RobotsTxtHealthCheck check = CreateCheck();

            // act
            _ = (await check.GetStatusAsync()).Single();
            _ = check.ExecuteAction(new HealthCheckAction("addDefaultRobotsTxtFile", check.Id));
            _ = (await check.GetStatusAsync()).Single();

            // assert
            Assert.That(textService.Calls, Is.Not.Empty);
            Assert.That(textService.Calls, Has.All.Property("Culture").EqualTo(testCulture));
            Assert.That(textService.Calls.Select(call => call.Alias),
                Does.Contain("seoRobotsCheckFailed")
                    .And.Contain("seoRobotsRectifyButtonName")
                    .And.Contain("seoRobotsRectifyDescription")
                    .And.Contain("seoRobotsCheckSuccess"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Test]
    public async Task Status_endpoint_reports_a_status()
    {
        // arrange
        HttpClient client = ReferenceSiteFixture.Client;

        // act
        HttpResponseMessage response = await client.GetAsync("/example/health-check/robots");

        // assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Match("^(Error|Success)\\|(True|False)$"));
    }

    [Test]
    public async Task Remediation_endpoint_requires_authorization()
    {
        // arrange
        HttpClient client = ReferenceSiteFixture.Client;

        // act
        HttpResponseMessage response = await client.PostAsync(
            "/example/health-check/robots?action=addDefaultRobotsTxtFile", content: null);

        // assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized)
            .Or.EqualTo(HttpStatusCode.NotFound));
    }

    private RobotsTxtHealthCheck CreateCheck(string? contentRoot = null) =>
        new(
            textService,
            new TestHostEnvironment(contentRoot ?? this.contentRoot),
            logger);

    private sealed class TestHostEnvironment(string contentRoot) : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "Umbraco-CMS.Skills.Tests";
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class RecordingTextService : ILocalizedTextService
    {
        public List<LocalizationCall> Calls { get; } = [];

        public string Localize(
            string? area,
            string? alias,
            CultureInfo? culture,
            IDictionary<string, string?>? tokens = null)
        {
            Calls.Add(new LocalizationCall(area, alias, culture));
            return $"{area}:{alias}";
        }

        public IDictionary<string, IDictionary<string, string>> GetAllStoredValuesByAreaAndAlias(
            CultureInfo culture) =>
            new Dictionary<string, IDictionary<string, string>>();

        public IDictionary<string, string> GetAllStoredValues(CultureInfo culture) =>
            new Dictionary<string, string>();

        public IEnumerable<CultureInfo> GetSupportedCultures() => [];

        public CultureInfo ConvertToSupportedCultureWithRegionCode(CultureInfo currentCulture) =>
            currentCulture;
    }

    private sealed record LocalizationCall(string? Area, string? Alias, CultureInfo? Culture);

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            NoopDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add(new LogEntry(logLevel, exception, formatter(state, exception)));
    }

    private sealed record LogEntry(LogLevel Level, Exception? Exception, string Message);

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
