# Rendering media

Showing images, video and files from the Media library, including Image Cropper crops and resizing.
See also [rendering-content.md](rendering-content.md).

**Docs (Umbraco 17, fetch before implementing):** [Rendering Media](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/templating-and-rendering/design/rendering-media.md) · [Image Cropper](https://docs.umbraco.com/umbraco-cms/17.latest/model-your-content/property-editors/built-in-umbraco-property-editors/image-cropper.md)

## Core rules

- **A media item is published content.** `Umbraco.Media(Guid)` returns it, with `Url()`, `Name`, and
  properties like `umbracoWidth` / `umbracoHeight` (images only; a `File` or `Video` doesn't have
  them).
- **Typed with ModelsBuilder:** `Umbraco.Media(id) as Image` gives `UmbracoWidth`, `UmbracoHeight`
  and so on.
- **Always null-check:** `Media()` can return null, and an `as` cast can fail.
- **Crops:** `@Url.GetCropUrl(mediaItem, "square")` uses a crop defined on the Image Cropper Data
  Type. **Max width, keeping the aspect ratio:** `mediaItem.GetCropUrl(width: 800)`, with no
  height or crop alias. The original image is just `mediaItem.Url()`.
- **Video:** render `Url()` in a `<video><source type="video/mp4"></video>` tag.
- Use the media's `Name`, or a dedicated alt-text property, for `alt`.
- Media is usually picked on content (Media Picker) rather than looked up by a hard-coded GUID.
  Render the picked value from the page.

## Steps

1. Find which property holds the media (Media Picker alias) and which crops exist.
2. Render with null checks, `GetCropUrl`, and meaningful `alt`.
3. Build if possible. Otherwise say it wasn't verified.

## Done

Tell the user which crops the markup relies on, so editors know to keep them on the Data Type.
