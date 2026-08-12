## Custom production HTTPS Health Check

Add this class to your Umbraco 17 web project, for example `HealthChecks/ProductionUseHttpsHealthCheck.cs`:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Umbraco.Cms.Core.HealthChecks;

namespace MySite.HealthChecks;

[HealthCheck(
    "8C7F3F95-2AF5-4F62-9B72-7AE5B1E9C4D1",
    "Production HTTPS configuration",
    Description = "Checks that Umbraco:CMS:Global:UseHttps is enabled when the site runs in Production.",
    Group = "Security")]
public sealed class ProductionUseHttpsHealthCheck : HealthCheck
{
    private const string ConfigurationPath = "Umbraco:CMS:Global:UseHttps";
    private const string DocumentationUrl =
        "https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check.md";

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public ProductionUseHttpsHealthCheck(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public override Task<IEnumerable<HealthCheckStatus>> GetStatusAsync()
    {
        if (!_environment.IsProduction())
        {
            return Task.FromResult<IEnumerable<HealthCheckStatus>>(
                new[]
                {
                    new HealthCheckStatus(
                        $"Skipped: this check applies only when the site environment is Production (current: {_environment.EnvironmentName}).")
                    {
                        ResultType = StatusResultType.Info,
                        ReadMoreLink = DocumentationUrl,
                    },
                });
        }

        var useHttps = _configuration.GetValue<bool?>(ConfigurationPath) == true;
        var status = new HealthCheckStatus(
            useHttps
                ? $"Umbraco:CMS:Global:UseHttps is enabled in Production."
                : $"Umbraco:CMS:Global:UseHttps must be true in Production.")
        {
            ResultType = useHttps ? StatusResultType.Success : StatusResultType.Error,
            ReadMoreLink = DocumentationUrl,
        };

        return Task.FromResult<IEnumerable<HealthCheckStatus>>(new[] { status });
    }
}
```

### Why this implementation

- The `HealthCheck` attribute gives the check a stable GUID, display name, description, and the existing **Security** group.
- `GetValue<bool?>` treats a missing setting as not enabled, so Production cannot accidentally pass because the key is absent or malformed.
- Non-Production environments are reported as informational and are not treated as a production failure.
- The check is read-only: it does not attempt to modify `appsettings.json` or provide an unsafe automatic fix.
- The `ReadMoreLink` points to Umbraco's official Health Check documentation, which specifically documents the `UseHttps` setting.

Place the class in the web project's assembly. Umbraco discovers `HealthCheck` implementations in the application and displays this one under **Settings > Health Check > Security**; no controller or Razor view is required.

### Verify it in the backoffice

1. Set `Umbraco:CMS:Global:UseHttps` to `true` in the production configuration source (for example, the production appsettings file or environment variable `Umbraco__CMS__Global__UseHttps=true`).
2. Run the site with `ASPNETCORE_ENVIRONMENT=Production` and sign in to the backoffice.
3. Open **Settings > Health Check**, expand **Security**, and locate **Production HTTPS configuration**.
4. Click/run the check. It should show **Success** when the value is true.
5. Temporarily set the value to `false` (or remove it), restart/reload the site so configuration is re-read, and run the check again. It should show **Error** and the official documentation link.
6. In Development or Staging, it should show **Info** stating that the production-only check was skipped.

Build verification: not run. No project input or buildable application was provided, and repository files were not modified.
