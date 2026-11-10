# Using search and analytics evidence

Read this only when the user supplies search or analytics evidence: a Search Console export, an
analytics export (Google Analytics, Matomo or similar) or results from another connected data
tool. The CMS shows what a page says; this evidence shows what readers and search engines did.

## Before using it

- **Name the source, period, filters and grain** at the top of the report: which tool, which date
  range, which country or device filters, and whether rows are per page, per query or per
  page and query.
- **Check the page link.** Query data is only evidence about a page if the export is filtered to
  that page or has a page column. If it is not, say so and do not attach queries to pages.
- **Keep missing values unknown.** An empty cell or a page missing from the export is unknown, not
  zero.

## What it can add

| Evidence | Use it to |
|---|---|
| Queries with impressions for a page | Find the questions readers search for, and check whether the page answers them |
| High impressions, low click-through | Suggest the title or meta description may not match the query (a hypothesis) |
| Landing sessions and engagement | Rank which pages matter most for the audit |
| Two periods | Size a change. Do not explain its cause from this data alone |

## Rules

- **Correlation is not cause.** A drop in clicks after an edit does not prove the edit caused it.
  Seasonality, ranking changes and tracking changes are possible too.
- **A low click-through rate alone does not prove a title problem.** Position, the search result
  layout and the query mix all affect it.
- **Sessions are not citations.** Visits from an AI assistant do not show what it cited, and query
  wording does not prove a query came from AI.
- **Keep the evidence apart from the CMS findings** in the report, so the user can see which
  findings rest on data and which on the content alone.
