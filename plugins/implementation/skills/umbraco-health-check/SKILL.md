---
name: umbraco-health-check
description: >
  Create and localize custom Umbraco 17 Health Checks for backend C# and Razor frontend
  experiences, including multilingual Dictionary items and safe fallback handling. Use this skill
  when the user asks to add a custom Umbraco Health Check, validate an appsettings value, show
  health-check guidance in Razor, localize a check, organise its Dictionary items, or handle a
  missing translation safely. SKIP: non-Umbraco projects, Umbraco versions below 17, existing
  built-in checks that only need configuration, and liveness/readiness or orchestrator probe
  endpoints.
---

# Umbraco Health Check

Use this skill for the Settings > Health Check dashboard and any explicitly requested Razor
presentation of its guidance or status text. Fetch the official documentation before implementing;
do not copy API samples from memory.

## Choose the implementation surface

| Surface | Choose when | Guidance |
|---|---|---|
| **Backend C#** (default) | The requirement is a dashboard check, configuration comparison, runtime inspection, or safe remediation action | Use the current Umbraco 17 `AbstractSettingsCheck` or `HealthCheck` API; keep checks fast and report failures honestly. |
| **Razor frontend** | The user explicitly wants localized health-check guidance or status copy rendered by a view/component | Keep status computation in C#; use the project’s Razor/localization conventions, encode output, and never expose secrets. |

If the request is only for a backoffice check, do not add a Razor surface. If it asks for
Kubernetes, load-balancer, liveness, or readiness probes, skip this skill and use the official
[Health Probes documentation](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/server-setup/health-probes.md).

## Backend C# and official source

Start with the official
[Health Check documentation](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check.md).
Use `AbstractSettingsCheck` for one configuration key and accepted values; use `HealthCheck` for
custom runtime logic, multiple statuses, or an explicit `HealthCheckAction`. Generate a stable GUID,
use an appropriate group, validate action aliases, and keep remediation idempotent. Check the
project’s actual Umbraco 17 package and namespace conventions before writing code.

For scheduling and notification delivery, consult the separate
[Health checks configuration documentation](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/configuration/healthchecks.md);
do not conflate scheduled notifications with dashboard execution.

## Razor, multilingual Dictionary items, and fallbacks

When Razor is requested, inspect the existing view/component and localization conventions first.
Keep user-facing text in Dictionary items rather than hard-coded translated strings. Organise items
by feature and message purpose, use stable aliases, and create values deliberately for every
supported culture. Follow the project’s established culture-selection mechanism.

Define fallback behavior before implementation: requested culture, configured/default culture, then
a safe non-sensitive invariant message. A missing Dictionary item or translation must produce a
visible controlled fallback or an explicit failure state—not a blank success, leaked configuration
value, swallowed exception, or silent default. Encode Razor output and never render secrets or raw
configuration values.

## Validation

Objective scenarios and build-honesty requirements live in
[`evals/evals.json`](evals/evals.json). Build a real project when one exists and state clearly when
verification was not possible.
