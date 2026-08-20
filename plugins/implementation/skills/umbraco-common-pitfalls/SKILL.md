---
name: umbraco-common-pitfalls
description: >
  Reference guide to anti-patterns, performance traps, and common mistakes in Umbraco
  development. Covers DI vs. statics, static references to request-scoped instances,
  DescendantsOrSelf() on large trees, over-querying, services in Razor views, volatile content
  nodes, startup processing, Examine N+1, RenderTemplateAsync misuse, constructor logic, eager
  loading, missing cache, memory pressure, and Models Builder misuse.
  Use this whenever the user asks about "common pitfalls", "performance issues", "anti-patterns",
  "memory leaks", "best practices for Umbraco code", "avoid mistakes in Umbraco",
  "why is my Umbraco site slow", or asks to review or audit Umbraco code for correctness.
  SKIP: non-Umbraco projects.
---

# Common Pitfalls & Anti-Patterns

The patterns below cover the most impactful mistakes in Umbraco development — issues that cause
memory leaks, instability, N+1 queries, and poor performance. When reviewing or writing Umbraco
code, check each applicable pitfall and apply the fix before claiming correctness. Source of
truth: [official Umbraco docs](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/application-code/common-pitfalls.md).

---

### 1. Singletons and statics

Umbraco provides DI everywhere. Static fields and Service Locator calls make code untestable,
create API leakage, and introduce lifetime mismatches. Use constructor injection instead — all
Umbraco controllers, composers, notification handlers, and Razor base classes support it.

---

### 2. Static references to request-scoped instances

`UmbracoHelper` and `UmbracoContext` are **request-scoped**: they live for one HTTP request.
Storing them in a static or singleton field traps a request's cache snapshot and user security
context in application memory — causing memory leaks and cross-request data bleed.

**Bad:**
```csharp
public class BadApiController : Controller
{
    private static UmbracoHelper _umbracoHelper; // static + request-scoped = leak

    public BadApiController(IUmbracoHelperAccessor accessor)
    {
        if (_umbracoHelper is null)
        {
            accessor.TryGetUmbracoHelper(out var helper);
            _umbracoHelper = helper;
        }
    }
}
```

**Good:** inject `IUmbracoHelperAccessor` and resolve per-request, or inject `UmbracoHelper`
directly (it is registered as request-scoped in DI and is safe when consumed that way).

---

### 3. DescendantsOrSelf() on large trees

`DescendantsOrSelf()` iterates **every node** in the subtree. On a 10,000-node site, using it
to build a nav menu iterates all 10,000 nodes even when only level-2 children are needed.

**Bad:**
```cshtml
@foreach (var node in Model.Root().DescendantsOrSelf().Where(x => x.Level == 2))
```

**Good:**
```cshtml
@foreach (var node in Model.Root().Children())
```

Use `DescendantsOrSelf()` only when the subtree is provably small and filtering at depth is
genuinely required.

---

### 4. Over-querying (repeated traversals)

Every `.Root()`, `.Ancestor()`, or property resolution is a cache traversal. Calling
`Model.Root()` three times traverses upward three times.

**Bad:**
```cshtml
<a href="@Model.Root().Url()">@Model.Root().Name</a>
@foreach (var node in Model.Root().Children()) { ... }
```

**Good:**
```cshtml
@{ var root = Model.Root(); }
<a href="@root.Url()">@root.Name</a>
@foreach (var node in root.Children()) { ... }
```

---

### 5. Using the Services layer in Razor views

`IContentService`, `IMediaService`, `IMemberService`, and similar services hit the **database
directly**. In a Razor view they bypass the published content cache, slow rendering, and can
cause unintended writes.

**Bad:**
```cshtml
@inject IContentService _contentService
@{ var item = _contentService.GetById(1234); }
```

**Good:**
```cshtml
@{ var item = Umbraco.Content(1234); }
```

Read-only APIs that are safe in views: `UmbracoHelper` (`@Umbraco.*`), `ITagQuery`,
`IMemberManager`.

---

### 6. Volatile data stored as Umbraco content nodes

Umbraco's publish/index/cache pipeline is not designed for high-frequency writes. Using content
nodes for hit counters, form submissions, or bulk imports degrades performance and stability.

| Don't do this | Use instead |
|---|---|
| Hit counter on a content node | Google Analytics or a custom DB table |
| New content node per form submission | Custom DB table |
| Bulk data import into content nodes | Custom DB tables; surface via content if needed |

---

### 7. Expensive processing during startup

Code in `UmbracoApplicationStartingNotification` handlers runs synchronously during boot. Slow
startup hurts cold starts and every application restart.

**Good:** lazy-load instead:
```csharp
private readonly Lazy<ExpensiveResource> _resource = new(() => BuildExpensiveResource());
```

Or use `LazyInitializer.EnsureInitialized`. For one-time DB operations (e.g. creating a schema
table), set a persistence flag so the work is skipped on subsequent restarts.

---

### 8. Rebuilding Examine indexes unnecessarily

Index rebuilds iterate every content and media item and can cause out-of-memory on large sites.
Keep Umbraco and Examine up to date; that resolves most sync issues without manual rebuilds.

Primary causes of index drift: outdated Umbraco version; rebuilding while simultaneously
restarting the app domain.

---

### 9. Service lookups inside Examine events

`TransformingIndexValues` and `DocumentWriting` fire for **every document being indexed**. A
service call inside one of these events becomes an N+1 problem — once per document, multiplied
by every rebuild.

**Bad:**
```csharp
private void OnTransformingIndexValues(object sender, IndexingItemEventArgs e)
{
    var content = _contentService.GetById(int.Parse(e.ValueSet.Id)); // N+1
}
```

**Good:** use the data already present in `e.ValueSet.Values` rather than fetching from the
service layer. For data that truly isn't in the index, batch-load it before the event fires.

---

### 10. Using RenderTemplateAsync for content rendering

`RenderTemplateAsync` renders a template to a string — designed for scenarios like email
generation. Using it for on-page content modules causes severe performance problems.

**Good:** render reusable content blocks with Partial Views:
```cshtml
@await Html.PartialAsync("_MyPartial", model)
```

Or use View Components for anything that requires its own service resolution.

---

### 11. Logic in constructors

Constructors should only set fields and validate parameters. LINQ operations such as `Select`,
`OrderBy`, or `Where` may instantiate objects thousands of times — if the constructor performs
expensive work, the cost multiplies.

**Bad:**
```csharp
public RecipeModel(IPublishedContent content, IPublishedValueFallback fallback)
    : base(content, fallback)
{
    // Runs for every object LINQ touches, including ones that are later discarded
    RelatedRecipes = content.Parent()
        .Children<RecipeModel>()
        .Where(x => x.Value<IEnumerable<int>>("related").Contains(content.Id));
}
```

**Good:** use lazy-loaded properties (see pitfall 12).

---

### 12. Eager loading — use lazy loading instead

Resolve property values only when actually accessed. The `??=` null-coalescing assignment is the
idiomatic pattern:

```csharp
private int? _votes;
public int Votes => _votes ??= this.Value<int>("votes");

private List<int> _related;
public IEnumerable<int> RelatedRecipes =>
    _related ??= this.Value<IEnumerable<int>>("related").ToList();
```

Return IDs, not resolved `IPublishedContent` instances. Storing resolved entities on a cached
model bloats the content cache.

---

### 13. Not caching expensive lookups

If the same content item (global nav root, settings node) is needed on every request, cache or
hardcode its ID and retrieve via `Umbraco.Content(id)`. A direct ID lookup is a single cache
dictionary hit; tree traversal is not.

---

### 14. Memory pressure from excessive object allocation

Creating thousands of wrapper objects via LINQ `Select` generates garbage-collector pressure.
Large allocations promoted to Generation 2/3 are expensive to collect and can cause application
pauses.

Prefer querying `IPublishedContent` directly rather than wrapping every node in a custom model:

```cshtml
@foreach (var recipe in recipeNode.Children()
    .OrderByDescending(x => x.Value<int>("votes"))
    .Take(10))
```

---

### 15. Models Builder misuse

Use ModelsBuilder partial classes to add **stateless, local** features — computed properties
derived from the model's own data. Do not:

- Transform content into view models inside a ModelsBuilder partial
- Resolve and store related content as properties
- Manage or traverse content trees

These concerns belong in controllers, view components, or services.

---

## Version compatibility

The official docs page covering these pitfalls targets **Umbraco 17 and 18** — the only versions
for which it is currently published. The underlying patterns apply to any DI-era Umbraco (v9+),
but the documentation source is only verified against 17/18.

Currently active supported versions (as of 2026-08-05):

| Version | Type | End-of-Life |
|---|---|---|
| Umbraco 18 | STS | 25-06-2027 |
| Umbraco 17 | LTS | 27-11-2028 |
| Umbraco 13 | LTS | 14-12-2026 |

Versions 10–12 and 14–16 are end-of-life. Umbraco 13 remains supported until 14-12-2026. Source:
[Umbraco LTS & End-of-Life](https://umbraco.com/products/knowledge-center/long-term-support-and-end-of-life/).

## Documentation reference

- [Common Pitfalls & Anti-Patterns](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/application-code/common-pitfalls.md) — source of truth for all patterns in this skill

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`.
