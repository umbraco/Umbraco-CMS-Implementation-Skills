# Transcript: unpublish-url-410 (with skill)

## Files read
- plugins/implementation/skills/umbraco-services/SKILL.md
- references/using-built-in-services.md (the task is a notification handler, so this is the matching reference. custom-services.md was not needed beyond what the composer asset shows.)
- assets/UnpublishingUrlRecorder.cs, assets/SiteServiceComposer.cs
- evals/ was not read.

## Fetched
- v17 Services and Helpers doc (docs.umbraco.com/umbraco-cms/17.latest/.../services.md). The user said Umbraco 17 and there was no project, so the version came from the prompt. The doc confirms it uses ContentUnpublishedNotification for "accessing the cache before unpublish", which is the error the skill's "Docs corrections" section flags.

## Key reasoning
- Use ContentUnpublishingNotification (pre-event, page still cached), not Unpublished (post-event, cache already updated, GetById returns null on 17.5.3 according to the skill's runtime gate).
- There's no front-end request in a backoffice-triggered handler, so use IUmbracoContextFactory.EnsureUmbracoContext() in a using, not IUmbracoContextAccessor.
- Read the URL from the cache with IPublishedUrlProvider. Read side: cache, not IContentService.
- Store is a singleton (app-wide state, no scoped dependencies). Handler is registered with builder.AddNotificationHandler in an IComposer.
- Adapted the asset's store to be keyed by URL, because the content finder needs lookup by request path. Flagged this as an unverified change. Also flagged persistence/load balancing, republish, descendants and multi-culture caveats.
- No build was possible (no project), and the answer says so.
