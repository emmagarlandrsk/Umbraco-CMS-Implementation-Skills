# Custom production HTTPS setting health check

Add this class to your Umbraco web project (for example, `HealthChecks/ProductionHttpsSettingCheck.cs`):

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Umbraco.Cms.Core.HealthChecks;

namespace MySite.HealthChecks;

[HealthCheck(
    "7E8F0B1A-4D0D-4C12-9AA1-3B6DA1E56D2F",
    "Production HTTPS setting",
    Description = "Verifies that Umbraco:CMS:Global:UseHttps is enabled in production.",
    Group = "Security")]
public sealed class ProductionHttpsSettingCheck : HealthCheck
{
    private const string SettingPath = "Umbraco:CMS:Global:UseHttps";
    private const string DocumentationLink =
        "https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check";

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    public ProductionHttpsSettingCheck(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
    }

    public override Task<IEnumerable<HealthCheckStatus>> GetStatusAsync()
    {
        if (!_hostEnvironment.IsProduction())
        {
            return Task.FromResult<IEnumerable<HealthCheckStatus>>(
                new[]
                {
                    new HealthCheckStatus("Skipped: this check applies only to the Production environment.")
                    {
                        ResultType = StatusResultType.Info
                    }
                });
        }

        var enabled = _configuration.GetValue<bool?>(SettingPath) == true;
        var status = new HealthCheckStatus(
            enabled
                ? $"{SettingPath} is true in Production."
                : $"{SettingPath} must be true in Production.")
        {
            ResultType = enabled ? StatusResultType.Success : StatusResultType.Error,
            ReadMoreLink = enabled ? null : DocumentationLink
        };

        return Task.FromResult<IEnumerable<HealthCheckStatus>>(new[] { status });
    }

    public override HealthCheckStatus ExecuteAction(HealthCheckAction action) =>
        throw new NotSupportedException("This configuration must be changed in appsettings or the hosting environment.");
}
```

Set the value in the production configuration source (for example, `appsettings.Production.json`, environment variables, or your secret/configuration provider):

```json
{
  "Umbraco": {
    "CMS": {
      "Global": {
        "UseHttps": true
      }
    }
  }
}
```

The `HealthCheck` attribute supplies the dashboard name, unique ID, description, and `Group = "Security"`. The check deliberately reports **Info** outside Production, and treats a missing value as false in Production. The failure result exposes Umbraco's official Health Check documentation link, which explains custom checks and the `UseHttps` setting.

## Verify it in the backoffice

1. Run the site with `ASPNETCORE_ENVIRONMENT=Production` and the production configuration above.
2. Sign in to the Umbraco backoffice with an account that can access Settings.
3. Open **Settings** and select **Health Check**.
4. Expand **Security**, locate **Production HTTPS setting**, and run/refresh the check.
5. Confirm it shows a green **Success** message stating that `UseHttps` is true.
6. Temporarily set the effective production value to `false` (or remove it), restart if your configuration provider requires it, and run the check again. Confirm it shows **Error**, says the setting must be true, and offers the official documentation link.
7. Restore `true`. In a non-production environment, confirm the same check appears under **Security** with an **Info/Skipped** result.

This is a code-only answer; no project files were modified and no build or test was run.
