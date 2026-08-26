using System.Globalization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Web.HealthCheck.Checks.SEO;

[HealthCheck("3A482719-3D90-4BC1-B9F8-910CD9CF5B32", "Robots.txt",
    Description = "Create a robots.txt file to block access to system folders.",
    Group = "SEO")]
public class RobotsTxtHealthCheck : Umbraco.Cms.Core.HealthChecks.HealthCheck
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

    public override Task<IEnumerable<HealthCheckStatus>> GetStatusAsync() =>
        Task.FromResult<IEnumerable<HealthCheckStatus>>(new[] { CheckForRobotsTxtFile() });

    public override HealthCheckStatus ExecuteAction(HealthCheckAction action) =>
        action.Alias switch
        {
            AddDefaultRobotsTxtAction => AddDefaultRobotsTxtFile(),
            _ => throw new InvalidOperationException($"Action '{action.Alias}' is not supported.")
        };

    private HealthCheckStatus CheckForRobotsTxtFile()
    {
        var robotsTxtPath = Path.Combine(_hostEnvironment.ContentRootPath, "robots.txt");
        var exists = File.Exists(robotsTxtPath);
        var message = exists
            ? _textService.Localize("healthcheck", "seoRobotsCheckSuccess", CultureInfo.CurrentUICulture)
            : _textService.Localize("healthcheck", "seoRobotsCheckFailed", CultureInfo.CurrentUICulture);

        var actions = new List<HealthCheckAction>();
        if (!exists)
        {
            actions.Add(new HealthCheckAction(AddDefaultRobotsTxtAction, Id)
            {
                Name = _textService.Localize("healthcheck", "seoRobotsRectifyButtonName", CultureInfo.CurrentUICulture),
                Description = _textService.Localize("healthcheck", "seoRobotsRectifyDescription", CultureInfo.CurrentUICulture)
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
            File.WriteAllText(Path.Combine(_hostEnvironment.ContentRootPath, "robots.txt"), content);
            return new HealthCheckStatus(
                _textService.Localize("healthcheck", "seoRobotsCheckSuccess", CultureInfo.CurrentUICulture))
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
        new(_textService.Localize("healthcheck", "seoRobotsRectifyFailed", CultureInfo.CurrentUICulture))
        {
            ResultType = StatusResultType.Error,
            Actions = new List<HealthCheckAction>()
        };
}