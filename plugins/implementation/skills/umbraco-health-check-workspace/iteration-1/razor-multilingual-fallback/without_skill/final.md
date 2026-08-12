# Localized health-check warning (Umbraco 17 Razor)

Create one Dictionary item with the key `HealthCheck.Warning` and add these language values:

- English: `Some health checks need attention. Please try again later.`
- Danish: `Nogle sundhedstjek kræver opmærksomhed. Prøv igen senere.`
- German: `Einige Gesundheitsprüfungen erfordern Aufmerksamkeit. Bitte versuchen Sie es später erneut.`

Render the warning only when the health-check result requires it. `GetDictionaryValue` is given a fallback, and the extra check also protects against an empty value (or an implementation that returns the key when no translation exists):

```cshtml
@using System.Globalization
@{
    // Replace this with the real result from your health-check code.
    var healthCheckNeedsAttention = ViewData["HealthCheckNeedsAttention"] as bool? == true;
    var dictionaryKey = "HealthCheck.Warning";
    var fallback = "Some health checks need attention. Please try again later.";

    var warning = healthCheckNeedsAttention
        ? Umbraco.GetDictionaryValue(dictionaryKey, fallback)
        : null;

    if (healthCheckNeedsAttention &&
        (string.IsNullOrWhiteSpace(warning) ||
         string.Equals(warning, dictionaryKey, StringComparison.Ordinal)))
    {
        warning = fallback;
    }
}

@if (!string.IsNullOrWhiteSpace(warning))
{
    <aside class="health-check-warning" role="alert">
        @warning
    </aside>
}
```

Ensure the request culture is set to `en`, `da`, or `de` before the view renders (using Umbraco’s configured request-culture/localization middleware). Dictionary lookup uses that current culture; the English text is the deliberate safe fallback, so a missing Danish or German translation never leaves visitors with a blank warning or a dictionary key.

No repository files were modified. Build verification was not run because this response only supplies Razor and configuration guidance.
