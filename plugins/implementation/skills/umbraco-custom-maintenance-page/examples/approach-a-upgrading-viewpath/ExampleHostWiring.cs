using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Skills.Examples.CustomMaintenancePage;

/// <summary>
/// This example's own harness — not part of the skill.
///
/// Stands in for the appsettings.json edit a real user makes by hand: set UpgradingViewPath to the
/// custom maintenance view and make sure the maintenance page is shown during upgrade state.
/// </summary>
public class ExampleHostWiringComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.PostConfigure<GlobalSettings>(settings =>
        {
            settings.UpgradingViewPath = "~/Views/maintenance.cshtml";
            settings.ShowMaintenancePageWhenInUpgradeState = true;
        });
    }
}
