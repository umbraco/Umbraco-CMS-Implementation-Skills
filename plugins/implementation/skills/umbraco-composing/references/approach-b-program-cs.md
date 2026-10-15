# Approach B — `Program.cs`

## When to choose this approach

- You own `Program.cs` (site code, not a package).
- A registration has to win over one made by a composer, especially one in a third-party package
  whose composer type you can't reference for `[ComposeAfter]`.
- The team wants every Umbraco registration visible in one file.

For packages, class libraries, or when no ordering guarantee is needed, use
[Approach A](approach-a-composer.md).

## Why the placement after `.AddComposers()` matters

`.AddComposers()` runs every composer immediately, at the point where it appears in the chain, and
each composer's service and collection changes are applied as it runs. So a call placed **after**
`.AddComposers()` has two effects:

- Its `builder.Services` changes are applied after every composer's. That means `AddUnique`
  replaces whatever any composer registered.
- Its collection changes are applied after every composer's, so `Remove`/`Replace`/`InsertBefore`
  can target items that a composer added.

Placed **before** `.AddComposers()`, the call runs before the composers, and they can override it.
That's rarely what you want.

## Building blocks (fetch the docs before implementing)

- **Registering in `Program.cs`, and bundling registrations in extension methods**:
  [Dependency Injection](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/application-code/backend-and-custom-logic/using-ioc.md)

## Steps

1. **Discover project context.** Find the namespace and the Umbraco version (17+). Read the existing
   `CreateUmbracoBuilder()` chain in `Program.cs`. If a composer already calls `Add<Feature>()`,
   delete it (or its call) when moving to this approach, so the registrations aren't duplicated.
2. **Write `FeatureBuilderExtensions.cs`**, substituting placeholders and adapting it as described
   in [approach-a-composer.md](approach-a-composer.md#the-files). Don't write `FeatureComposer.cs`.
3. **Call it from `Program.cs`, after `.AddComposers()` and before `.Build()`:**

   ```csharp
   builder.CreateUmbracoBuilder()
       .AddBackOffice()
       .AddWebsite()
       .AddComposers()
       .Add<Feature>()   // after AddComposers: runs after every composer
       .Build();
   ```

   Keep the existing chain as it is, including `.AddDeliveryApi()` or any other calls already
   there. Only insert the one line. Add a `using <Namespace>;` if the extension's namespace isn't
   already imported.
4. **Ordering, replacing and startup work.** Apply
   [collections-and-ordering.md](collections-and-ordering.md) and
   [startup-and-runtime-levels.md](startup-and-runtime-levels.md) as the task requires.
5. **Build**, then boot if you can, because collection anchors are only checked at boot. If no
   buildable project exists, say so plainly, don't claim a verified build, and state whether you
   expect the code to compile against the Umbraco 17 APIs it uses.

## Done

Tell the user:

- The exact line added to `Program.cs` and its position relative to `.AddComposers()`, with the
  reason for that position.
- What it registers or overrides, including which composer's registration it now wins over.
