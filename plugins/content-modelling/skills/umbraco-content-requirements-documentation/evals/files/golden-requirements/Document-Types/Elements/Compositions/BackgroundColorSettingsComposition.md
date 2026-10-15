# Background Color Settings Composition

> **Status:** applied 2026-10-01 via MCP

> **Element type**: used inside block editors; not routable as a page.

## Definition

| Setting | Value |
|---|---|
| Name | Background Color Settings Composition |
| Alias | `backgroundColorSettingsComposition` |
| Kind | Element Type |
| Icon | `icon-settings` |
| Description | Adds a background colour choice to a block's settings. |
| Folder | [Document Types](../../../Document-Types.md) / [Elements](../../Elements.md) / [Compositions](../Compositions.md) |
| Allowed at root | No |
| Vary by culture | No |
| Allowed children | — |
| Compositions | — |

## Properties

| Tab | Tab Sort | Group | Group Sort | Name | Alias | Data Type | Editor | Value Type | Mandatory | Sort | Description |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Style | 50 | — | — | Background Color | `backgroundColor` | [Background Color Picker](../../../Data-Types/Color-Picker/Background-Color-Picker.md) | `Umbraco.ColorPicker` | `Umbraco.Cms.Core.PropertyEditors.ValueConverters.ColorPickerValueConverter.PickedColor` | No | 100 | Leave empty for the page background. |

## Used by

- Used as a composition by: [Accordion Settings](../Settings/AccordionSettings.md)

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| [Background Color Picker](../../../Data-Types/Color-Picker/Background-Color-Picker.md) | `data-type` | New in this changeset |
| [Compositions](../Compositions.md) *(document-type-container)* | `document-type-container` | New in this changeset |
