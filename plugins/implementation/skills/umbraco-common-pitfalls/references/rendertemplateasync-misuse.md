# Using RenderTemplateAsync for content rendering

**Symptoms:** pages built from content modules or blocks render very slowly.
**Look for:** `RenderTemplateAsync` used to render on-page content rather than for off-page output
such as emails.
**Docs section:** "RenderTemplateAsync"

`RenderTemplateAsync` renders a template to a string — designed for scenarios like email
generation. Using it for on-page content modules causes severe performance problems.

**Good:** render reusable content blocks with Partial Views:
```cshtml
@await Html.PartialAsync("_MyPartial", model)
```

Or use View Components for anything that requires its own service resolution.
