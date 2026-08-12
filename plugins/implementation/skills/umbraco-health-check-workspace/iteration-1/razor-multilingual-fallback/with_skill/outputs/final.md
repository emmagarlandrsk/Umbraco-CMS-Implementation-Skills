## Recommended implementation

Because the requirement is visitor-facing presentation, use the **Razor surface**; keep the health-check status computation in backend C# rather than in the view. Have an injected provider return a small, non-sensitive result (for example, `IsWarning` and a dictionary alias), and let the view only localize and render it.

### 1. Stable Dictionary items

Create these Dictionary items under a feature-oriented naming convention (aliases are not translated):

- `HealthCheck.Warning`
  - `en-US`: `A site health check needs attention.`
  - `da-DK`: `Et webstedshelbredstjek kræver opmærksomhed.`
  - `de-DE`: `Eine Website-Integritätsprüfung erfordert Aufmerksamkeit.`
- `HealthCheck.Warning.MissingTranslation`
  - `en-US`: `A site health warning is currently unavailable.`
  - `da-DK`: `En advarsel om webstedets sundhed er ikke tilgængelig lige nu.`
  - `de-DE`: `Eine Warnung zum Website-Status ist derzeit nicht verfügbar.`

Use the actual cultures configured by the site if their aliases differ from these examples.

### 2. Backend status model/provider

Keep the existing/custom check execution in C# and expose only a safe result to Razor:

```csharp
public sealed record HealthWarningState(bool IsWarning, string? MessageAlias);

public interface IHealthWarningProvider
{
    HealthWarningState GetCurrent();
}
```

The implementation should map a failed/attention state to `HealthCheck.Warning`; it must not pass configuration values, exception details, credentials, or other secrets to the view. Register the provider in DI and inject it into the view/component that renders the site-wide warning.

### 3. Razor fallback helper and rendering

The requested culture should be the current published-content/UI culture. Try that culture first, then the configured default (`en-US` below), then the invariant safe message. Treat a missing key or blank translation as missing. Razor's normal `string` rendering is HTML-encoded; do not use `Html.Raw` for dictionary text.

```cshtml
@using System.Globalization
@using Microsoft.AspNetCore.Localization
@using Microsoft.Extensions.Options
@inject IHealthWarningProvider HealthWarningProvider
@inject IOptions<RequestLocalizationOptions> LocalizationOptions

@functions {
    private string? GetDictionaryValue(string alias, CultureInfo culture)
    {
        var value = Umbraco.GetDictionaryValue(alias, null, culture);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, alias, StringComparison.Ordinal)
            ? null
            : value;
    }

    private string GetSafeWarning(string alias, CultureInfo requested, CultureInfo fallback)
    {
        return GetDictionaryValue(alias, requested)
            ?? GetDictionaryValue(alias, fallback)
            ?? GetDictionaryValue("HealthCheck.Warning.MissingTranslation", requested)
            ?? GetDictionaryValue("HealthCheck.Warning.MissingTranslation", fallback)
            ?? "A site health warning is currently unavailable.";
    }
}

@{
    var state = HealthWarningProvider.GetCurrent();
    var requestedCulture = CultureInfo.CurrentUICulture;
    var defaultCultureName = LocalizationOptions.Value.DefaultRequestCulture?.Culture.Name ?? "en-US";
    var defaultCulture = CultureInfo.GetCultureInfo(defaultCultureName);
    var warningText = state.IsWarning
        ? GetSafeWarning(state.MessageAlias ?? "HealthCheck.Warning", requestedCulture, defaultCulture)
        : null;
}

@if (warningText is not null)
{
    <aside class="health-warning" role="status" aria-live="polite">
        @warningText
    </aside>
}
```

If the project uses a different `GetDictionaryValue` overload, keep the same fallback algorithm but adapt that one call to the site's established Umbraco localization convention. Ensure the three supported cultures have deliberate values and test a fourth/unsupported culture plus a deliberately missing translation. The result must be a visible controlled fallback, never a blank success.

Official references:
- [Umbraco Health Check](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check.md)
- [Language Variation](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/templating-and-rendering/language-variation.md)
- [MVC Partial Views](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/templating-and-rendering/templating/mvc/partial-views.md)

**Build verification:** No project files or input implementation were provided, and I did not run a build. The snippets should be checked against the site's installed Umbraco 17 package and existing localization conventions before deployment.
