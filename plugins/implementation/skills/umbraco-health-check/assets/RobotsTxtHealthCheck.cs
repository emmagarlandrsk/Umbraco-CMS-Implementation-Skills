using Umbraco.Cms.Core.Extensions;
using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Web.HealthCheck.Checks.SEO;

[HealthCheck("3A482719-3D90-4BC1-B9F8-910CD9CF5B32", "Robots.txt",
    Description = "Create a robots.txt file to block access to system folders.",
    Group = "SEO")]
public class RobotsTxtHealthCheck : HealthCheck
{
    private const string AddDefaultRobotsTxtAction = "addDefaultRobotsTxtFile";
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<RobotsTxtHealthCheck> _logger;
    private readonly ILocalizedTextService _textService;

    public RobotsTxtHealthCheck(
        ILocalizedTextService textService,
        IHostEnvironment hostEnvironment,
        ILogger<RobotsTxtHealthCheck> logger)
    {
        _textService = textService;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public override Task<IEnumerable<HealthCheckStatus>> GetStatus() =>
        Task.FromResult<IEnumerable<HealthCheckStatus>>(new[] { CheckForRobotsTxtFile() });

    public override HealthCheckStatus ExecuteAction(HealthCheckAction action) =>
        action.Alias switch
        {
            AddDefaultRobotsTxtAction => AddDefaultRobotsTxtFile(),
            _ => throw new InvalidOperationException($"Action '{action.Alias}' is not supported.")
        };

    private HealthCheckStatus CheckForRobotsTxtFile()
    {
        var robotsTxtPath = _hostEnvironment.MapPathContentRoot("~/robots.txt");
        var exists = File.Exists(robotsTxtPath);
        var message = exists
            ? _textService.Localize("healthcheck", "seoRobotsCheckSuccess")
            : _textService.Localize("healthcheck", "seoRobotsCheckFailed");

        var actions = new List<HealthCheckAction>();
        if (!exists)
        {
            actions.Add(new HealthCheckAction(AddDefaultRobotsTxtAction, Id)
            {
                Name = _textService.Localize("healthcheck", "seoRobotsRectifyButtonName"),
                Description = _textService.Localize("healthcheck", "seoRobotsRectifyDescription")
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
        const string content = """
            # robots.txt for Umbraco
            User-agent: *
            Disallow: /umbraco/
            """;

        try
        {
            File.WriteAllText(_hostEnvironment.MapPathContentRoot("~/robots.txt"), content);
            return new HealthCheckStatus(
                _textService.Localize("healthcheck", "seoRobotsCheckSuccess"))
            {
                ResultType = StatusResultType.Success,
                Actions = new List<HealthCheckAction>()
            };
        }
        catch (IOException exception)
        {
            _logger.LogError(exception, "Could not write robots.txt to the root of the site.");
            return WriteFailureStatus();
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogError(exception, "Could not write robots.txt to the root of the site.");
            return WriteFailureStatus();
        }
    }

    private HealthCheckStatus WriteFailureStatus() =>
        new(_textService.Localize("healthcheck", "seoRobotsRectifyFailed"))
        {
            ResultType = StatusResultType.Error,
            Actions = new List<HealthCheckAction>()
        };
}
