# Checks: titles, meta descriptions, headings, alt text

What to check on each page and when a result is a finding. The CMS Editor MCP's SEO audit returns
these fields with flags; the flags are evidence, the judgement is yours. For what search engines
document about titles and snippets, fetch Google's
[SEO starter guide](https://developers.google.com/search/docs/fundamentals/seo-starter-guide)
rather than working from memory.

| Check | Finding when | Not a finding |
|---|---|---|
| SEO title | Missing; the same as another page's; does not say what the page is about | Over a character count but clear (Low at most) |
| Meta description | Missing; the same as another page's; generic ("Welcome to our website") | A description that is long but accurate (Low at most) |
| H1 | No H1 in content and none from the template; more than one H1 | The template writes the H1 from the page name: report once as a site pattern |
| Heading levels | Skipped levels (H2 to H4) in content | Headings a block template adds that the content data does not show: mark as unverified |
| Image alt text | A meaningful image (product, chart, person) with no alt text | A decorative image with empty alt on purpose |

## Rules

- **Duplicates need two pages.** A duplicated title or description is a finding on both pages;
  report it once with both pages listed.
- **Don't invent alt text.** Report the image and what it appears to be for; the editor who knows
  the image writes the text.
- **Templates change what visitors see.** The CMS holds content, not the rendered page. If a check
  depends on the template (H1 from the page name, a title suffix), say it is unverified and name the
  rendered page or preview as the way to confirm.
- **Length is a hint, not a rule.** Titles and descriptions are truncated by width, not by a fixed
  character count. Never make length alone a High finding.
