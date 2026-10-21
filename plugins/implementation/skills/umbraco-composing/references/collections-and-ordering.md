# Collections, replacing services, and ordering

There are three independent orderings, and mixing them up is the usual cause
of "my registration has no effect":

1. **Items inside a collection.** For example, where your content finder sits among Umbraco's.
2. **Composers relative to each other.**
3. **Composers relative to `Program.cs`**, which is covered in
   [approach-b-program-cs.md](approach-b-program-cs.md#why-the-placement-after-addcomposers-matters).

The collection list, the collection kinds and the attribute syntax are all in the
[Composing docs](https://docs.umbraco.com/umbraco-cms/model-your-content/content-types-and-structure/composing.md),
so fetch it before writing code. This file covers what the docs leave out.

## Contents

- [1 — Items inside a collection](#1--items-inside-a-collection): read when adding, inserting,
  replacing or removing a content finder, URL provider or other collection item, or when an anchor
  type makes boot fail.
- [Replacing a single service (not a collection)](#replacing-a-single-service-not-a-collection): read
  when overriding one service with `AddUnique`, including its lifetime.
- [2 — Composers relative to each other](#2--composers-relative-to-each-other): read when one composer
  must run before or after another.
- [Disabling a composer](#disabling-a-composer): read when turning off your own or a package's composer.

## 1 — Items inside a collection

Each collection's kind determines which methods it has, so check the kind in the docs table first:

| Kind | Methods | Order comes from |
|---|---|---|
| Ordered | `Append`, `Insert(index)`, `InsertBefore<TAnchor, T>`, `InsertAfter<TAnchor, T>`, `Replace<TOld, TNew>`, `Remove<T>`, `Clear` | The calls you make |
| Weighted | `Add`, `Remove`, `SetWeight<T>(int)`, `Clear` | `[Weight(n)]` on the type, or `SetWeight` |
| Set | `Add`, `Remove`, `Replace`, `Clear` | Not ordered |
| Lazy | `Add` (types, or a type-scan factory), `Exclude<T>`, `Clear` | Not ordered |

**Anchor to the core item, never to the list's position or to `Append`.** `Append` puts an item
after Umbraco's own items. For `ContentFinders` that means a URL the core finders already match
never reaches yours. `Insert()` with no index puts the item first, which is usually too early
(it runs before `ContentFinderByPageIdQuery`), unless the requirement really is to run before every
core finder.

**The anchor must already be in the collection when the call runs, or boot fails.** Ordered, Set and
Weighted builder calls change the list immediately. They aren't deferred to `.Build()`. So `InsertBefore`,
`InsertAfter` and `Replace` throw a bare `InvalidOperationException` ("Operation is not valid due to
the current state of the object.") right at the call: inside `.AddComposers()` for a composer, or
at the `Program.cs` line for Approach B. The message doesn't name the missing type, but the stack
trace runs through your `Add<Feature>()` and `InsertBefore`, and that's the clue. If the anchor is
an item another composer adds, your composer needs `[ComposeAfter]` on that composer (see below).
Otherwise it can run first and throw. Use the types that are actually registered in the running
version. In Umbraco 17:

| Collection | Registered by default, in order |
|---|---|
| `ContentFinders()` | `ContentFinderByPageIdQuery`, `ContentFinderByUrlNew`, `ContentFinderByKeyPath`, `ContentFinderByUrlAlias`, `ContentFinderByRedirectUrl` |
| `UrlProviders()` | `AliasUrlProvider`, `NewDefaultUrlProvider` |

`ContentFinderByUrl` and `DefaultUrlProvider` still compile, with only a CS0618 obsolete warning,
but they are **not registered**. So anchoring to them builds and then fails at boot. For any other
collection, or if this table may be stale, read the registrations in Umbraco's `UmbracoBuilder.Collections.cs` for the
installed version rather than guessing.

The 404 "last chance" finder isn't part of `ContentFinders()`. It's a single service set with
`builder.SetContentLastChanceFinder<T>()`, which replaces Umbraco's own. See the
[`umbraco-custom-error-pages`](../../umbraco-custom-error-pages/SKILL.md) skill.

**To drop or swap one built-in item**, use the collection's own method from the table above:
`Remove<T>()` (`Exclude<T>()` on a Lazy collection such as `HealthChecks()`) or
`Replace<TOld, TNew>()`. A Lazy collection applies its type scans and `Exclude`s at `.Build()`, so
`Exclude<T>()` works from any composer, whatever the order. Don't `Clear()` and rebuild it: that silently drops items that later
Umbraco versions or other packages add.

**Type scanning** (a Lazy collection fed by `builder.TypeLoader.GetTypes<T>()`) slows boot.
Register types explicitly where you can. If you must scan, make the scanned interface extend
`IDiscoverable` so the result is cached.

## Replacing a single service (not a collection)

Use `builder.Services.AddUnique<TService, TImplementation>(lifetime)` from `Umbraco.Extensions`.
It removes every existing registration of `TService`, then adds yours.

- **Pass the original lifetime.** Without the argument, `AddUnique` registers a **singleton**.
  Replacing a scoped or transient service that way changes its lifetime, and a singleton that
  captures scoped state misbehaves across requests.
- `AddSingleton`/`AddScoped` don't replace anything. They add a second registration, so constructor
  injection gets yours (last wins) but `IEnumerable<TService>` consumers get both.
- It only wins if it runs **after** the original registration. Core Umbraco services are registered
  before `.AddComposers()`, so any composer can replace them. A service registered by another
  composer needs `[ComposeAfter]` (below) or [Approach B](approach-b-program-cs.md).

## 2 — Composers relative to each other

With no attributes, the order among composers is **unspecified**. It falls out of type-scan order,
and it can change when assemblies are added.

- `[ComposeAfter(typeof(OtherComposer))]` makes this composer run after `OtherComposer`.
  `[ComposeBefore(typeof(OtherComposer))]` makes it run before.
- `ComposeAfter` on a class is a **strong** requirement. If `OtherComposer` is missing or disabled,
  boot fails with `Broken composer dependency`. When the other composer is optional (for example, a
  package that might not be installed), pass `weak: true` (`[ComposeAfter(typeof(OtherComposer), true)]`).
- The attributes aren't inherited by subclasses. Circular requirements fail boot.
- If you can't reference the other composer's type at all, use Approach B, which runs after every
  composer.

## Disabling a composer

Disabling a composer turns off **everything** it registers. If you only need to undo one item,
prefer `Remove`/`Exclude`/`Replace`/`AddUnique` and leave the rest.

- `[Disable]` on a composer disables that composer.
- `[Disable(typeof(TheirComposer))]` on your composer disables theirs. This is useful when you're
  replacing a package's feature wholesale.
- `[assembly: DisableComposer(typeof(TheirComposer))]` does the same at assembly level, with no
  composer of your own needed.

Any composer with a strong `[ComposeAfter]` on the disabled one now fails boot (see above).
