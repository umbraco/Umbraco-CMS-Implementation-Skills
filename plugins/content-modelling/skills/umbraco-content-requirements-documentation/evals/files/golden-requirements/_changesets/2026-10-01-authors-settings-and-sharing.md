# Changeset: Authors, site settings and block styling

> **Status:** applied 2026-10-01 via MCP

## Summary

Adds an Authors data folder and the picker articles use to choose an author, a site settings singleton at the content root, an Open Graph composition grouped beside the SEO fields, and a background colour settings model for the Accordion block. Four existing pages are updated: the SEO composition gains a group, the main content grid registers the settings model, and the article and home pages take the new composition.

## Requirements pages

| Order | Requirements page | Kind | Action |
|---|---|---|---|
| 1 | [Background Color Picker](../Data-Types/Color-Picker/Background-Color-Picker.md) | Data type | Create |
| 2 | [Background Color Settings Composition](../Document-Types/Elements/Compositions/BackgroundColorSettingsComposition.md) | Settings composition | Create |
| 3 | [Accordion Settings](../Document-Types/Elements/Settings/AccordionSettings.md) | Settings model | Create |
| 4 | [Main Content Block Grid](../Data-Types/Block-Grid/Main-Content-Block-Grid.md) | Block Grid data type | Update |
| 5 | [Open Graph Composition](../Document-Types/Compositions/OpenGraphComposition.md) | Composition | Create |
| 6 | [SEO Composition](../Document-Types/Compositions/SeoComposition.md) | Composition | Update |
| 7 | [Author](../Document-Types/Data/Author.md) | Document type | Create |
| 8 | [Author Folder](../Document-Types/Data/AuthorFolder.md) | Document type | Create |
| 9 | [Author Picker](../Data-Types/Content-Picker/Author-Picker.md) | Data type | Create |
| 10 | [Article Page](../Document-Types/ArticlePage.md) | Document type | Update |
| 11 | [Home Page](../Document-Types/HomePage.md) | Document type | Update |
| 12 | [Site Settings](../Document-Types/SiteSettings.md) | Document type | Create |

## Apply checklist

- [x] 1. Create data type folder `Color Picker` and data type `Background Color Picker`
- [x] 2. Create document type folder `Elements/Compositions`, then element `backgroundColorSettingsComposition`, move it into `Elements/Compositions`, then fix-up (Style tab sort 50)
- [x] 3. Create document type folder `Elements/Settings`, then element `accordionSettings`, move it into `Elements/Settings`, then fix-up (compositions)
- [x] 4. Update data type `Main Content Block Grid`: register `accordionSettings` as the settings element of `accordion`
- [x] 5. Create composition `openGraphComposition`, then fix-up (group Sharing 100, sorts, descriptions)
- [x] 6. Update composition `seoComposition`: move its fields into group `Search` (0), keeping every property
- [x] 7. Create document type folder `Data`, then document type `author` in `Data`, then fix-up
- [x] 8. Create document type `authorFolder` in `Data`, then fix-up (allowed at root)
- [x] 9. Set allowed children on `authorFolder`: `author`
- [x] 10. Create the `Authors` content node at the content root, sorted after the home page (content, by the user)
- [x] 11. Create data type folder `Content Picker` and data type `Author Picker` with the `Authors` node as its start node (after step 10, so the node exists)
- [x] 12. Update document type `articlePage`: add property `author` and composition `openGraphComposition`, keeping everything else
- [x] 13. Update document type `homePage`: add composition `openGraphComposition`, keeping everything else
- [x] 14. Create document type `siteSettings`, then fix-up (tabs General 0 and Footer 1, allowed at root, no template)
- [x] 15. Create the `Site Settings` content node at the content root, sorted after the home page (content, by the user)
- [x] 16. Verify every requirements doc against the site and set each status line

## Apply log

—
