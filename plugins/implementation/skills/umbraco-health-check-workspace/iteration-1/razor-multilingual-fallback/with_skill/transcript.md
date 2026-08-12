Task: Provide an Umbraco 17 Razor implementation for a localized visitor-facing health-check warning in English, Danish, and German, using Dictionary items and a safe missing-translation fallback. No input files; do not modify repository source files.

Actions taken:
- Loaded the Umbraco health-check skill context and inspected its SKILL.md and eval expectations.
- Selected the Razor surface because the request explicitly asks for frontend presentation.
- Followed the skill requirement to keep status computation in backend C# and make Razor responsible only for localization/rendering.
- Fetched the official Umbraco Health Check documentation and relevant language-variation/partial-view documentation endpoints.
- Produced stable feature/message-oriented Dictionary aliases with deliberate en-US, da-DK, and de-DE values.
- Defined an explicit fallback chain: requested UI culture, configured default culture, localized missing-translation message, then a safe invariant message.
- Ensured the rendering path does not expose configuration or exception data and relies on Razor HTML encoding rather than Html.Raw.
- Recorded that no build was run because no project/input implementation was supplied.

Output written to:
plugins/implementation/skills/umbraco-health-check-workspace/iteration-1/razor-multilingual-fallback/with_skill/outputs/final.md
