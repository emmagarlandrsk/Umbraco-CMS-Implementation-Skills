using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace Umbraco_CMS.Skills.TestHost.Blank;

/// <summary>
/// Deterministic runtime validation of umbraco-custom-maintenance-page — the UpgradingViewPath approach.
///
/// Proves the custom .cshtml renders with a 503 when the runtime level is flipped to Upgrading, which
/// is the state an unattended upgrade puts the site in. The IRuntimeState is only changed for this
/// single test and reset in a finally.
///
/// Runs on SITE 2 because the approach is a template + config approach. It shares the blank host with
/// other Approach-B fixtures; the runtime-level flip is temporary and reset, so other tests see Run.
/// </summary>
[TestFixture]
public class MaintenancePageBlankTests
{
    private static HttpClient Client => BlankSiteFixture.Client;

    [Test]
    public async Task Maintenance_page_renders_with_503_when_runtime_is_upgrading()
    {
        IRuntimeState runtimeState = BlankSiteFixture.Factory.Services
            .GetRequiredService<IRuntimeState>();

        // Flip to Upgrading, the state the skill is designed for.
        runtimeState.Configure(RuntimeLevel.Upgrading, RuntimeLevelReason.UpgradeMigrations);

        try
        {
            HttpResponseMessage response = await Client.GetAsync("/");
            string body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable),
                $"front-end requests must receive HTTP 503 while the runtime is Upgrading. "
                + $"Got {response.StatusCode}: {body[..Math.Min(500, body.Length)]}");

            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"),
                "the maintenance view must be returned as text/html");

            Assert.That(body, Does.Contain("We'll be right back"),
                "the configured UpgradingViewPath must render the custom maintenance view");

            Assert.That(body, Does.Contain("noindex"),
                "the maintenance page must tell crawlers not to index it");
        }
        finally
        {
            // Restore normal runtime so the shared host's other fixtures are not affected.
            runtimeState.Configure(RuntimeLevel.Run, RuntimeLevelReason.Run);
        }
    }
}
