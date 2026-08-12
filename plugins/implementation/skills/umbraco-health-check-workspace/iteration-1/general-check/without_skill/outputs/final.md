# Umbraco 17 robots.txt Health Check

Add this general (runtime) Health Check to the web project. It checks the physical content root, reports an explicit success/error status, and exposes a create action only while `robots.txt` is absent.

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Umbraco.Cms.Core.Extensions;
using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Cms.Core.Services;

namespace MySite.HealthChecks;

[HealthCheck(
    "9C6E4A7D-3D2E-4A45-8CB9-4B2B8D0F0D41",
    "Robots.txt",
    Description = "Checks that robots.txt exists at the site content root.",
    Group = "SEO")]
public sealed class RobotsTxtHealthCheck : HealthCheck
{
    private const string CreateAction = "createRobotsTxt";
    private const string RelativePath = "~/robots.txt";
    private const string DefaultContent = "# robots.txt for Umbraco\nUser-agent: *\nDisallow: /umbraco/\n";

    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILocalizedTextService _text;
    private readonly ILogger<RobotsTxtHealthCheck> _logger;

    public RobotsTxtHealthCheck(
        IHostEnvironment hostEnvironment,
        ILocalizedTextService text,
        ILogger<RobotsTxtHealthCheck> logger)
    {
        _hostEnvironment = hostEnvironment;
        _text = text;
        _logger = logger;
    }

    public override Task<IEnumerable<HealthCheckStatus>> GetStatus()
        => Task.FromResult<IEnumerable<HealthCheckStatus>>(new[] { GetRobotsStatus() });

    public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
    {
        if (!string.Equals(action.Alias, CreateAction, StringComparison.Ordinal))
            throw new InvalidOperationException($"Unsupported action '{action.Alias}'.");

        var path = _hostEnvironment.MapPathContentRoot(RelativePath);
        if (File.Exists(path))
            return Success(); // A concurrent request already created it; do not overwrite it.

        try
        {
            // CreateNew prevents this remediation from replacing an existing file in a race.
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream);
            writer.Write(DefaultContent);
            return Success();
        }
        catch (IOException) when (File.Exists(path))
        {
            return Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not create robots.txt at {Path}", path);
            return new HealthCheckStatus(
                _text.Localize("healthcheck", "seoRobotsCreateFailed"))
            {
                ResultType = StatusResultType.Error,
                Actions = Array.Empty<HealthCheckAction>()
            };
        }
    }

    private HealthCheckStatus GetRobotsStatus()
    {
        if (File.Exists(_hostEnvironment.MapPathContentRoot(RelativePath)))
            return Success();

        return new HealthCheckStatus(_text.Localize("healthcheck", "seoRobotsMissing"))
        {
            ResultType = StatusResultType.Error,
            Actions = new[]
            {
                new HealthCheckAction(CreateAction, Id)
                {
                    Name = _text.Localize("healthcheck", "seoRobotsCreateAction"),
                    Description = _text.Localize("healthcheck", "seoRobotsCreateDescription")
                }
            }
        };
    }

    private HealthCheckStatus Success() => new(
        _text.Localize("healthcheck", "seoRobotsExists"))
    {
        ResultType = StatusResultType.Success,
        Actions = Array.Empty<HealthCheckAction>()
    };
}
```

`MapPathContentRoot` is Umbraco's extension over `Microsoft.Extensions.Hosting.IHostEnvironment`; do not map this path from a request URL.

## Localization

Add the following keys to the project's user language file (for example `Config/Lang/en-US.user.xml`) and provide the same stable aliases in every supported culture file:

```xml
<area alias="healthcheck">
  <key alias="seoRobotsExists">robots.txt exists at the site content root.</key>
  <key alias="seoRobotsMissing">robots.txt is missing from the site content root.</key>
  <key alias="seoRobotsCreateAction">Create robots.txt</key>
  <key alias="seoRobotsCreateDescription">Creates a default robots.txt without replacing an existing file.</key>
  <key alias="seoRobotsCreateFailed">robots.txt could not be created. Check the application write permissions and logs.</key>
</area>
```

Use translated values—not translated aliases—in `da-DK.user.xml`, `de-DE.user.xml`, etc. The fallback behavior should be the project's normal `ILocalizedTextService` fallback; the failure text is deliberately non-sensitive.

## Scheduling and notification

Dashboard loading runs `GetStatus()` on demand. Scheduling/notification is separate: configure the Health Checks notification runner in `appsettings.json`:

```json
{
  "Umbraco": {
    "CMS": {
      "HealthChecks": {
        "Notification": {
          "Enabled": true,
          "FirstRunTime": "0 4 * * *",
          "Period": "1.00:00:00",
          "NotificationMethods": {
            "email": {
              "Enabled": true,
              "Verbosity": "Detailed",
              "FailureOnly": true,
              "Settings": { "RecipientEmail": "alerts@example.com" }
            }
          }
        }
      }
    }
  }
}
```

Configure SMTP as required by the site. This scheduled runner invokes the checks independently of someone opening Settings > Health Check; `FailureOnly` sends only failing results. A custom notification channel must implement `IHealthCheckNotificationMethod` (or derive from `NotificationMethodBase`) and use its own registered alias/settings.

Official references:
- https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check.md
- https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/configuration/healthchecks.md

## Verification

No repository files were changed and no build/test command was run. Build verification is therefore unavailable. In a real Umbraco 17 project, compile the web project, open **Settings > Health Check**, confirm the missing-file error and **Create robots.txt** action, run it, then verify the success status and the physical root file. Test a denied content-root write path to confirm the logged error status.
