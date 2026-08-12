Task: produce a response for an Umbraco 17 custom Health Check that verifies robots.txt, offers safe remediation, localizes messages, and explains scheduling/notifications.

Actions performed:
- Did not invoke or use an authoring/evaluator skill.
- Inspected the repository only to confirm the existing health-check context and official documentation URLs.
- Consulted the official Umbraco Health Check and Health Checks configuration documentation for the current general-check shape, action handling, localization mechanism, and notification settings.
- Did not modify repository source files.
- Wrote the proposed answer to outputs/final.md.

Implementation decisions summarized:
- Use Umbraco's general HealthCheck API with a stable GUID and SEO group.
- Check ~/robots.txt through Umbraco's content-root mapping.
- Return explicit Success/Error statuses.
- Offer the create action only when missing; validate the action alias in ExecuteAction.
- Use FileMode.CreateNew to avoid overwriting a file created concurrently.
- Log write failures and return a localized error instead of claiming success.
- Localize all user-facing status/action text through ILocalizedTextService and stable language aliases.
- Explain that scheduled notifications are independent from dashboard loading and show built-in email configuration.
- State honestly that no build or tests were run because the task forbade repository changes and no validation command was executed.
