# Transcript: readonly-api-contentservice (without skill, baseline)

- Fetched: nothing (no web fetches, no repo files read). Answered from model knowledge.
- Key reasoning:
  1. Spotted the classic anti-pattern: IContentService (DB-backed management API) used for front-end reads.
  2. Main risks: DB hit per request, cache bypassed; GetPagedChildren returns unpublished/draft items; GetValue returns draft values by default (it could leak unpublished prices); no property value converters.
  3. Recommended the published cache: IPublishedContentQuery.Content(Guid) → Children()/Children<Product>() → Value<decimal?>("price"), in a plain ControllerBase with [Route("api/products")] (UmbracoApiController is obsolete in 14+).
  4. Extra points: look up the folder by GUID key, Descendants for nested products, paging/output caching, culture.
  5. Alternative: the built-in Content Delivery API (fetch=children + filter=contentType).
  6. Flagged unverified API signatures (v15+ hybrid cache changed navigation extensions).
- Outputs: outputs/response.md
