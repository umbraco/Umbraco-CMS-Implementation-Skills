namespace <Namespace>.Models;

/// <summary>
/// The view model's nullability IS its contract with the view/JSON consumer. Decide per member:
///   - non-nullable + `required` → the mapper must always supply it (the compiler enforces this at
///     every `new`, so there is no CS8618 and no `= null!` placeholder);
///   - non-nullable + a default  → always safe to render; "no value" is represented as "";
///   - nullable (`string?`)       → absence is meaningful and the consumer must handle it
///     (e.g. render no &lt;img&gt; at all rather than an &lt;img src=""&gt;).
/// Never declare a member non-nullable and then assign it from a `T?` with `!` — that only moves the
/// NullReferenceException from the mapper into the view.
/// </summary>
public sealed class NullSafeContentViewModel
{
    public required Guid Key { get; init; }

    /// <summary>IPublishedContent.Name is declared non-nullable, so it can be copied straight across.</summary>
    public required string Name { get; init; }

    /// <summary>Always has a value: falls back to <see cref="Name"/> when the editor left it blank.</summary>
    public required string Title { get; init; }

    /// <summary>Optional text: "" when blank, never null.</summary>
    public string Subtitle { get; init; } = string.Empty;

    /// <summary>Null for a root node — there is no parent to name.</summary>
    public string? ParentName { get; init; }

    /// <summary>Null when no media was picked, or the picked media was deleted/unpublished.</summary>
    public string? ImageUrl { get; init; }

    /// <summary>Null when nothing was picked, or the picked node is no longer published.</summary>
    public string? PickedContentName { get; init; }
}
