---
name: umbraco-custom-maintenance-page
description: >
  Customize the page shown to visitors while Umbraco runs upgrade migrations. Covers Umbraco 17+
  upgrade pages, including the unattended upgrade view introduced for Umbraco 17.3+, the documented
  UmbracoWebsite maintenance page convention, and branded holding pages for Cloud and hosting downtime. Trigger: user asks to "customize
  maintenance page", "brand maintenance page", "create custom maintenance page", "style upgrade
  page", "change maintenance page in Umbraco", or "customize upgrading view". SKIP: non Umbraco
  projects, normal 404/500/Boot Failed pages, and general deployment holding pages without an
  Umbraco upgrade. Use umbraco-custom-error-pages for application errors.
---

# Custom Maintenance Page

Targets Umbraco 17 and later. The `UpgradingViewPath` guidance applies to Umbraco 17.3 and
later, where unattended migrations run in the background. Earlier Umbraco 17 versions and other
upgrade modes must follow the version specific official documentation. This is not an application
error page. Use `umbraco-custom-error-pages` for 404, 500, and Boot Failed handling.

## Choose the right Umbraco approach

| Situation | Configuration or file | Guidance |
|---|---|---|
| Umbraco 17.3+ unattended upgrades | `Umbraco:CMS:Global:UpgradingViewPath` and a custom Razor view | Preferred for the current background upgrade flow. Point the setting at a view such as `~/Views/MyUpgrading.cshtml`. |
| The documented generic maintenance page convention | `UmbracoWebsite/maintenance.cshtml` at the project root | Follow the official custom maintenance page tutorial when this convention matches the project's upgrade flow and version. |

Fetch the official documentation first. Do not assume the two paths are interchangeable without
checking the project's Umbraco version and upgrade mode.

## Choose the right hosting layer

| Layer | Use when | Implementation |
|---|---|---|
| **Umbraco runtime** | Umbraco is running an upgrade or migration | `UpgradingViewPath` or the documented `UmbracoWebsite/maintenance.cshtml` convention |
| **Umbraco Cloud platform** | Cloud infrastructure or platform operations make the app unavailable | Cloud portal Error Pages feature |
| **Hosting or web server** | The app is offline, restarting, or cannot start | `app_offline.htm`, deployment slots, or a web server fallback |

Do not use the Umbraco runtime page as a general deployment or app restart page. Offer the
appropriate hosting layer when the user is describing ordinary deployment downtime.

## Umbraco runtime implementation

Fetch the [unattended upgrade documentation](https://docs.umbraco.com/umbraco-cms/17.latest/get-started/upgrading-and-migrating/upgrade-unattended.md)
and the [global settings documentation](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/configuration/globalsettings.md)
first for Umbraco 17.3 and later. The current unattended flow serves `Upgrading.cshtml` with HTTP
503 while the RuntimeLevel is `Upgrading`. Customize it by setting:

```json
{
  "Umbraco": {
    "CMS": {
      "Global": {
        "UpgradingViewPath": "~/Views/MyUpgrading.cshtml"
      }
    }
  }
}
```

Create the configured Razor view with self contained markup. Offer
[`assets/maintenance.cshtml`](assets/maintenance.cshtml) as a starting template and tell the
user to copy it to the configured view path, or to the root level
`UmbracoWebsite/maintenance.cshtml` location when following the generic tutorial.

For the current Umbraco readiness flow, poll the anonymous
`/umbraco/api/health/ready` endpoint rather than polling the front page. It returns 503 while the
application is not ready and 200 when it can receive normal traffic. Do not add a custom health
endpoint unless the project needs one for a separate reason.

Keep the served file self contained. Use inline CSS, no external assets, no layout, and no live
Umbraco or Razor data such as `IPublishedContent`. The content cache or database may be unavailable
while the page is being served. In particular, do not make the upgrading view query a backoffice
content node for its heading or message: this creates a hard dependency on the very services that
may be unavailable and can turn a useful 503 page into another failure.

### Editor-controlled copy (safe compromise)

Editors can still own the wording, but the copy must be **baked into the deployed view ahead of the
upgrade**. Create a small backoffice content node (for example, `maintenanceCopy`) with two required
properties such as `heading` and `message`. A deployment/publish step that runs while the site is
healthy reads the published values, HTML-encodes them, and replaces explicit tokens in a copy of
`maintenance.cshtml` (for example, `<!-- MAINTENANCE_HEADING -->` and
`<!-- MAINTENANCE_MESSAGE -->`). Deploy that generated, self-contained file to the configured
`UpgradingViewPath` along with the application. Use safe fallback text when either value is missing.

This is not live content rendering: an editor's change takes effect on the next successful
publish/deployment that regenerates the file. Keep the last known good generated file in the
artifact; never require a backoffice request, `IPublishedContent`, a scope, or a database connection
while `RuntimeLevel` is `Upgrading`. See
[`references/editor-controlled-copy.md`](references/editor-controlled-copy.md) for the workflow and
trade-offs.

The official documentation warns against keeping a project in upgrade mode longer than necessary.
Test on staging with a real version upgrade path. Do not claim to have verified the page unless an
actual build or upgrade test was run.

## Cloud and hosting layers

For consistent branding, adapt the same self contained visual design for each layer:

- **Umbraco Cloud:** fetch the [Cloud Error Pages documentation](https://docs.umbraco.com/umbraco-cloud/build-and-customize-your-solution/handle-deployments-and-environments/error-pages.md)
  before giving portal instructions. The page is uploaded through the portal, assigned per
  hostname, and must be self contained and within the documented size limit. Offer
  [`assets/cloud-error-page.html`](assets/cloud-error-page.html) as a starter.
- **Azure or IIS:** use [`app_offline.htm`](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/app-offline)
  or deployment slots. Fetch the [Azure deployment slots documentation](https://learn.microsoft.com/en-us/azure/app-service/deploy-staging-slots)
  when recommending that approach.

Cloud platform downtime and ordinary app restart downtime require a hosting layer page. They are
not covered by the Umbraco runtime upgrade view.

## Disabling instead

Most upgrades do not require the site to go down. If the team prefers not to show the maintenance
page, add `Umbraco:CMS:Global:ShowMaintenancePageWhenInUpgradeState: false` in `appsettings.json`
using the official documentation for the project's version.

## Done

Tell the user:

- Which Umbraco approach applies to their version and upgrade mode.
- Where the configured view or maintenance file belongs.
- That the page appears during upgrade state and disappears when migrations finish.
- That the readiness endpoint can be used for safe automatic refresh where supported.
- That Cloud platform downtime and ordinary app restart downtime need their own hosting layer.

## Validation

Assertions for this skill live in [`evals/evals.json`](evals/evals.json). Run them with the
`umbraco-skill-evaluator` skill and validate the skill with `umbraco-skill-validator` and
`umbraco-skill-code-analyzer` before shipping.
