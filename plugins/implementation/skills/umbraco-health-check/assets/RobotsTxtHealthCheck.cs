using System.Globalization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Web.HealthCheck.Checks.SEO;

[HealthCheck("A7D3E9F1-60B4-4C8A-B2D5-9E1F73C6428B", "Robots.txt",
    Description = "Create a robots.txt file to block access to system folders.",
    Group = "SEO")]
public class RobotsTxtHealthCheck : Umbraco.Cms.Core.HealthChecks.HealthCheck
{
    private const string AddDefaultRobotsTxtAction = "addDefaultRobotsTxtFile";
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
        var robotsTxtPath = Path.Combine(_hostEnvironment.ContentRootPath, "robots.txt");

        if (File.Exists(robotsTxtPath))
        {
            return SuccessStatus();
        }

        try
        {
            using var stream = new FileStream(robotsTxtPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream);
            writer.Write(content);
            return SuccessStatus();
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

    private HealthCheckStatus SuccessStatus() =>
        new(_textService.Localize("healthcheck", "seoRobotsCheckSuccess", CultureInfo.CurrentUICulture))
        {
            ResultType = StatusResultType.Success,
            Actions = new List<HealthCheckAction>()
        };

    private HealthCheckStatus WriteFailureStatus() =>
        new(_textService.Localize("healthcheck", "seoRobotsRectifyFailed", CultureInfo.CurrentUICulture))
        {
            ResultType = StatusResultType.Error,
            Actions = new List<HealthCheckAction>()
        };
}