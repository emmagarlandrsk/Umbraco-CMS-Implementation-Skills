# Robots.txt Umbraco 17 Health Check

Because no project files were supplied, the following is the implementation to add to an Umbraco 17 project (for example, `HealthChecks/RobotsTxtHealthCheck.cs`). It uses the general dashboard Health Check API, not a liveness/readiness probe.

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Extensions;
using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Cms.Core.Services;

namespace MySite.HealthChecks;

[HealthCheck(
    "5F7D2A10-3DA8-4D4F-9C6E-9D7C0B3F2A41",
    "Robots.txt",
    Description = "Checks that the site has a robots.txt file.",
    Group = "SEO")]
public sealed class RobotsTxtHealthCheck : HealthCheck
{
    private const string AddActionAlias = "addRobotsTxt";
    private const string DefaultRobotsTxt = "# robots.txt for Umbraco\nUser-agent: *\nDisallow: /umbraco/\n";

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
    {
        var path = _hostEnvironment.MapPathContentRoot("~/robots.txt");
        var exists = File.Exists(path);

        var status = new HealthCheckStatus(_text.Localize(
            "healthcheck",
            exists ? "seoRobotsCheckSuccess" : "seoRobotsCheckFailed"))
        {
            ResultType = exists ? StatusResultType.Success : StatusResultType.Error,
            Actions = exists
                ? Array.Empty<HealthCheckAction>()
                : new[]
                {
                    new HealthCheckAction(AddActionAlias, Id)
                    {
                        Name = _text.Localize("healthcheck", "seoRobotsCreateActionName"),
                        Description = _text.Localize("healthcheck", "seoRobotsCreateActionDescription")
                    }
                }
        };

        return Task.FromResult<IEnumerable<HealthCheckStatus>>(new[] { status });
    }

    public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
    {
        if (!string.Equals(action.Alias, AddActionAlias, StringComparison.Ordinal))
            throw new InvalidOperationException("Action not supported.");

        var path = _hostEnvironment.MapPathContentRoot("~/robots.txt");

        // Re-check at execution time and use CreateNew so an existing file is never overwritten.
        if (File.Exists(path))
        {
            return Success();
        }

        try
        {
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream);
            writer.Write(DefaultRobotsTxt);
            return Success();
        }
        catch (IOException) when (File.Exists(path))
        {
            // Another request created it concurrently; the desired state is already satisfied.
            return Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not create robots.txt at {Path}.", path);
            return new HealthCheckStatus(_text.Localize("healthcheck", "seoRobotsCreateActionFailed"))
            {
                ResultType = StatusResultType.Error,
                Actions = Array.Empty<HealthCheckAction>()
            };
        }
    }

    private HealthCheckStatus Success() => new(
        _text.Localize("healthcheck", "seoRobotsCreateActionSuccess"))
    {
        ResultType = StatusResultType.Success,
        Actions = Array.Empty<HealthCheckAction>()
    };
}
```

Add the aliases to `Config/Lang/en-US.user.xml` (and provide deliberate translations for every supported language):

```xml
<area alias="healthcheck">
  <key alias="seoRobotsCheckSuccess">robots.txt exists.</key>
  <key alias="seoRobotsCheckFailed">robots.txt is missing from the site root.</key>
  <key alias="seoRobotsCreateActionName">Create robots.txt</key>
  <key alias="seoRobotsCreateActionDescription">Creates a conservative robots.txt that allows crawling except for /umbraco/. It will not overwrite an existing file.</key>
  <key alias="seoRobotsCreateActionSuccess">robots.txt was created, or was already created by another request.</key>
  <key alias="seoRobotsCreateActionFailed">robots.txt could not be created. Check the application identity's write permission for the site root and review the server log.</key>
</area>
```

The action is exposed only for a missing file, validates its alias in `ExecuteAction`, re-checks the file, and uses `FileMode.CreateNew` so it cannot overwrite an administrator's file. Failures are logged and returned as errors.

## Scheduling and email notification

Dashboard loading executes the check immediately; scheduling is separate. Configure the built-in notification runner in `appsettings.json`:

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

This runs daily after the configured first run and emails only failures. Configure SMTP as required by the site. Verify interactively under **Settings → Health Check**; the check should be an SEO check, show an error/action when absent, and show success after the action.

Official references:
- https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check.md
- https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/configuration/healthchecks.md

**Build verification:** not run. No project files were provided and this task requested no repository modifications.
