Task summary:
- Read the requested umbraco-health-check SKILL.md.
- Retrieved the official Umbraco 17 Health Check documentation and current v17 source for HealthCheck, HealthCheckAttribute, AbstractSettingsCheck, AcceptableConfiguration, and HealthCheckStatus.
- Chose a general HealthCheck rather than AbstractSettingsCheck because the requirement is conditional on the Production environment.
- Produced a read-only C# check that reads Umbraco:CMS:Global:UseHttps, requires true in Production, reports Info outside Production, uses the Security group, assigns a stable GUID, and exposes the official Health Check documentation link.
- Included concrete backoffice verification steps under Settings > Health Check > Security, including success, failure, and non-production cases.
- Did not modify repository files. Did not run a build because no input project was supplied; the final answer states this honestly.
