# <Name>

> **Status:** proposed

## Definition

| Setting | Value |
|---|---|
| Name | <Name> |
| Property editor | `Umbraco.ListView` |
| Editor UI | `Umb.PropertyEditorUi.Collection` |
| Database type | `Nvarchar` |
| Folder | <FolderBreadcrumb> |

## Configuration

Page size: <PageSize>.
Ordered by `<OrderByAlias>` <AscOrDesc>.
Columns: <ColumnAliases>.

| Setting | Value |
|---|---|
| `icon` | `<Icon>` |
| `tabName` | `<TabName>` |

**`layouts`**

| name | icon | collectionView |
|---|---|---|
| `Table` | `icon-list` | `Umb.CollectionView.Document.Table` |
| `Grid` | `icon-grid` | `Umb.CollectionView.Document.Grid` |

## Used by

Used as the list view for: <ListingPageLink>

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| <ArtifactLinkOrName> | `<ArtifactType>` | <Flag> |
