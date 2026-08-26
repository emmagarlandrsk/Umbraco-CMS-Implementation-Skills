using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Skills.Examples.HealthCheck;

public sealed class ExampleHealthCheckComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddTransient<ExampleHealthCheckController>();
    }
}

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ExampleHealthCheckController : Controller
{
    private readonly HealthCheckCollection _checks;
    private readonly IHostEnvironment _hostEnvironment;

    public ExampleHealthCheckController(
        HealthCheckCollection checks,
        IHostEnvironment hostEnvironment)
    {
        _checks = checks;
        _hostEnvironment = hostEnvironment;
    }

    [HttpGet]
    [Route("example/health-check/robots")]
    public async Task<IActionResult> Robots()
    {
        Umbraco.Cms.Core.HealthChecks.HealthCheck check =
            _checks.Single(x => x.Id == Guid.Parse("3A482719-3D90-4BC1-B9F8-910CD9CF5B32"));
        HealthCheckStatus status = (await check.GetStatusAsync()).Single();

        return Content(
            $"{status.ResultType}|{System.IO.File.Exists(Path.Combine(_hostEnvironment.ContentRootPath, "robots.txt"))}");
    }

    [HttpPost]
    [Route("example/health-check/robots")]
    public IActionResult ExecuteRobotsAction([FromQuery(Name = "action")] string? requestedAction = null)
    {
        if (string.IsNullOrWhiteSpace(requestedAction))
        {
            return BadRequest("An action alias is required.");
        }

        Umbraco.Cms.Core.HealthChecks.HealthCheck check =
            _checks.Single(x => x.Id == Guid.Parse("3A482719-3D90-4BC1-B9F8-910CD9CF5B32"));

        try
        {
            HealthCheckStatus status = check.ExecuteAction(new HealthCheckAction(requestedAction, check.Id));
            return Content(
                $"{status.ResultType}|{System.IO.File.Exists(Path.Combine(_hostEnvironment.ContentRootPath, "robots.txt"))}");
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete]
    [Route("example/health-check/robots")]
    public IActionResult RemoveRobotsFile()
    {
        System.IO.File.Delete(Path.Combine(_hostEnvironment.ContentRootPath, "robots.txt"));
        return NoContent();
    }
}
