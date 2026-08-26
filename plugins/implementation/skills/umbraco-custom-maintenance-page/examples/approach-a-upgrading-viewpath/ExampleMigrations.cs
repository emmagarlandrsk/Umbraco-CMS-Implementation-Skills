using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Packaging;
using Umbraco.Skills.Examples.Fixtures;

namespace Umbraco.Skills.Examples.CustomMaintenancePage;

/// <summary>
/// Installs the umbraco-custom-maintenance-page example into the blank reference host, standing in
/// for the manual file copy the user would otherwise do.
/// </summary>
public class CustomMaintenancePagePlan : PackageMigrationPlan
{
    public CustomMaintenancePagePlan()
        : base("Umbraco Custom Maintenance Page (example)")
    {
    }

    protected override void DefinePlan()
        => To<ImportCustomMaintenancePage>(new Guid("e1000000-0000-4000-8000-000000000002"));
}

/// <summary>
/// Installs the skill's maintenance.cshtml as a template so the configured UpgradingViewPath renders
/// the exact file the user is told to copy.
///
/// Calls IPackagingService.InstallCompiledPackageData directly rather than the inherited
/// ImportPackage.FromXmlDataManifest(...).Do() builder, which silently does nothing on Umbraco 17.5.3
/// when the manifest is supplied as an XDocument.
/// </summary>
public class ImportCustomMaintenancePage : AsyncPackageMigrationBase
{
    private readonly IPackagingService _packagingService;

    public ImportCustomMaintenancePage(
        IPackagingService packagingService,
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        MediaUrlGeneratorCollection mediaUrlGenerators,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        IMigrationContext context,
        IOptions<PackageMigrationSettings> packageMigrationsSettings)
        : base(packagingService, mediaService, mediaFileManager, mediaUrlGenerators,
            shortStringHelper, contentTypeBaseServiceProvider, context, packageMigrationsSettings)
        => _packagingService = packagingService;

    protected override Task MigrateAsync()
    {
        const string packageXml = @"<umbPackage>
  <info>
    <package>
      <name>Umbraco Custom Maintenance Page (example)</name>
    </package>
  </info>
  <Templates>
    <Template>
      <Name>Custom Maintenance</Name>
      <Alias>maintenance</Alias>
      <Key>e1000000-0000-4000-8000-000000000002</Key>
      <Design></Design>
    </Template>
  </Templates>
</umbPackage>";

        XDocument manifest = XDocument.Parse(packageXml)
            .WithTemplateDesign(
                "maintenance",
                EmbeddedManifest.Text(typeof(ImportCustomMaintenancePage).Assembly, "maintenance.cshtml"));

        _packagingService.InstallCompiledPackageData(manifest);
        return Task.CompletedTask;
    }
}
