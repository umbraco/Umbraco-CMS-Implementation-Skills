# Background Color Picker

> **Status:** applied 2026-10-01 via MCP

## Definition

| Setting | Value |
|---|---|
| Name | Background Color Picker |
| Property editor | `Umbraco.ColorPicker` |
| Editor UI | `Umb.PropertyEditorUi.ColorPicker` |
| Database type | `Nvarchar` |
| Folder | [Data Types](../../Data-Types.md) / [Color Picker](../Color-Picker.md) |

## Configuration

Labels shown; the editor picks one of the site's background colours.

| Setting | Value |
|---|---|
| `useLabel` | `true` |
| `items` | `ffffff` White, `f4f4f4` Light grey, `1a1a2e` Navy |

## Used by

| Content type | Property | Alias |
|---|---|---|
| [Background Color Settings Composition](../../Document-Types/Elements/Compositions/BackgroundColorSettingsComposition.md) | Background Color | `backgroundColor` |

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| [Color Picker](../Color-Picker.md) *(data-type-container)* | `data-type-container` | New in this changeset |
