# Article Listing Page Collection View

> **Status:** approved

## Definition

| Setting | Value |
|---|---|
| Name | Article Listing Page Collection View |
| Property editor | `Umbraco.ListView` |
| Editor UI | `Umb.PropertyEditorUi.Collection` |
| Database type | `Nvarchar` |
| Folder | [Data Types](../../Data-Types.md) / [Collection View](../Collection-View.md) |

## Configuration

Page size: 10.
Ordered by `updateDate` desc.
Columns: `updateDate`, `creator`.

| Setting | Value |
|---|---|
| `icon` | `icon-list` |
| `tabName` | `Articles` |

**`layouts`**

| name | icon | collectionView |
|---|---|---|
| `Table` | `icon-list` | `Umb.CollectionView.Document.Table` |
| `Grid` | `icon-grid` | `Umb.CollectionView.Document.Grid` |

## Used by

Used as the list view for: [Article Listing Page](../../Document-Types/ArticleListingPage.md)

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| [Collection View](../Collection-View.md) *(data-type-container)* | `data-type-container` | New in this changeset |
