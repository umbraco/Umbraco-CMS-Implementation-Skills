# Approach A — Composer

## When to choose this approach

- The default for any new feature.
- The code lives outside the web project (a package, RCL or shared class library).
- You want a feature folder that is self-contained: delete the folder and the feature is gone.

If the registration must win over one made by another composer that you can't annotate (typically
a third-party package), and you own `Program.cs`, use [Approach B](approach-b-program-cs.md). The
alternative is `[ComposeAfter]` on this composer (see
[collections-and-ordering.md](collections-and-ordering.md)), which works if you can name that
composer's type.

## Building blocks (fetch the docs before implementing)

- **Composers, collections and the composer attributes**:
  [Composing](https://docs.umbraco.com/umbraco-cms/model-your-content/content-types-and-structure/composing.md).
- **Registering dependencies and service lifetimes**:
  [Dependency Injection](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/application-code/backend-and-custom-logic/using-ioc.md)
- **Notification handlers**:
  [Notification Handler](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/notifications/notification-handler.md)

## Docs corrections (Umbraco 17)

| Docs snippet | Problem | Use instead |
|---|---|---|
| Composing: components implement `IComponent` (`Initialize`/`Terminate`) | Obsolete in 17 (CS0618), scheduled for removal in 18 | `RuntimeAsyncComponentBase` or `IAsyncComponent`, per [startup-and-runtime-levels.md](startup-and-runtime-levels.md) |
| Composing: the Runtime Levels list | Omits `Upgrading` (17.3+), during which components and startup notifications are deferred | See [startup-and-runtime-levels.md](startup-and-runtime-levels.md#the-runtime-levels-that-matter) |
| Composing: modifying `HealthChecks()` with `Clear()` then re-adding | Right only for a deliberate allow-list, and it also drops checks that Umbraco or other packages add later | `HealthChecks().Exclude<T>()` for just the one check, per [collections-and-ordering.md](collections-and-ordering.md) |

## The files

Read the templates in [`../assets/`](../assets) as the source:

- `FeatureBuilderExtensions.cs` holds `Add<Feature>()`, which makes every registration for the
  feature. Each block is marked *Optional*: keep only the blocks the task needs (and their `using`
  lines), and point the kept ones at the user's real types. The template assumes
  `<Feature>PublishedHandler` and `<Feature>ContentFinder`, so rename those and swap the
  notification type as needed.
- `FeatureComposer.cs` is the composer itself: one line that calls `Add<Feature>()`.
- `FeatureComponent.cs` is only needed if the feature has startup/shutdown work. If it doesn't,
  delete its `Components().Append` line too.

## Steps

1. **Discover project context.** Find the root namespace, the Umbraco version (must be 17+), and
   the folder conventions (`Composers/`, `Features/<Name>/`, `Infrastructure/`). Check for an
   existing composer or `Add*()` extension for this feature, and extend it rather than adding a
   second one. Also check whether `Program.cs` already calls an `Add*()` for it. If so, that's
   Approach B, and you shouldn't add a composer as well.
2. **Write the files**, substituting `<Namespace>` and `<Feature>` and adapting
   `FeatureBuilderExtensions.cs` as described in [The files](#the-files).
3. **Ordering and replacing.** If the task inserts into a collection, replaces a service or must run
   after another composer, apply [collections-and-ordering.md](collections-and-ordering.md).
4. **Startup work.** If the task runs code at startup, apply
   [startup-and-runtime-levels.md](startup-and-runtime-levels.md).
5. **Build.** If a real `.csproj` is present, run `dotnet build` and fix any errors. Collection
   anchors are only checked when the host boots, so run the site too if you can. If no buildable
   project exists, say so plainly, don't claim a verified build, and state whether you expect the
   code to compile against the Umbraco 17 APIs it uses.

## Done

Tell the user:

- Which composer was added, and that it runs automatically, so there's no `Program.cs` change.
- What it registers, and where any collection item sits relative to Umbraco's own items.
- Any ordering dependency (`[ComposeAfter]`) and why it's needed.
- For startup work, the runtime levels it runs at.
