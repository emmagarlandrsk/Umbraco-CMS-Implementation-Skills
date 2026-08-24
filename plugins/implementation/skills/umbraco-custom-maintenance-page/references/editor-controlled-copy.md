# Editor-controlled maintenance copy

## Why the upgrading view cannot read the node live

In Umbraco 17.5 unattended upgrades, `UpgradingViewPath` is served while the runtime level is
`Upgrading` and the application may not be ready. The content cache and database can be unavailable.
A view that injects or queries `IPublishedContent` (or otherwise opens an Umbraco scope) is therefore
not a reliable maintenance page and must not be the primary implementation.

## Safe implementation: bake the published values into the file

1. Create and publish a small backoffice content node, such as `Maintenance copy`, with plain text
   `heading` and `message` properties. Restrict it to one location and give both properties sensible
   defaults.
2. During a healthy deployment or release build, use a deployment step or build utility that reads
   the published values from that node. Run this step before the application is put into upgrade
   state; do not run it from the upgrading view request.
3. HTML-encode the values and replace the contents between the marker pairs in
   `assets/maintenance.cshtml`:

   ```html
   <!-- MAINTENANCE_HEADING -->fallback heading<!-- /MAINTENANCE_HEADING -->
   <!-- MAINTENANCE_MESSAGE -->fallback message<!-- /MAINTENANCE_MESSAGE -->
   ```

   The replacement must preserve the markers or remove them, and must reject missing/invalid values
   in favour of the checked-in fallback copy. Do not insert raw editor HTML into this page.
4. Deploy the generated file to the path configured by
   `Umbraco:CMS:Global:UpgradingViewPath`, for example `~/Views/maintenance.cshtml`. Keep the last
   known-good generated file in the deployment artifact so the upgrade page has no database,
   backoffice, layout, or live Razor-data dependency.
5. Verify the generated artifact contains the expected encoded heading and message, then exercise
   the upgrade flow on staging. The view should still return HTML/HTTP 503 and its readiness polling
   should reload after `/umbraco/api/health/ready` returns 200.

An editor's change is reflected on the next successful publish/deployment that runs the generation
step, not immediately during an upgrade. If immediate backoffice-driven changes are a hard
requirement, use a separate healthy runtime page rather than the `UpgradingViewPath` endpoint and
accept that it cannot cover the period when Umbraco itself is unavailable.
