# Changeset: Article section

> **Status:** approved

## Summary

Adds an article section to a site with no page types of its own: a home page, an article listing page and an article page, with the two compositions, three block elements, five data types and four templates they need. The article body is a Block Grid that offers an accordion and a rich text block.

## Requirements pages

| Order | Requirements page | Kind | Action |
|---|---|---|---|
| 1 | [Toggle (default on)](../Data-Types/Toggle/Toggle-default-on.md) | Data type | Create |
| 2 | [Meta Description Text Area](../Data-Types/Text-Area/Meta-Description-Text-Area.md) | Data type | Create |
| 3 | [Page Details Composition](../Document-Types/Compositions/PageDetailsComposition.md) | Composition | Create |
| 4 | [SEO Composition](../Document-Types/Compositions/SeoComposition.md) | Composition | Create |
| 5 | [Accordion Item](../Document-Types/Elements/AccordionItem.md) | Element type | Create |
| 6 | [Rich Text](../Document-Types/Elements/RichText.md) | Element type | Create |
| 7 | [Accordion Items](../Data-Types/Block-List/Accordion-Items.md) | Block List data type | Create |
| 8 | [Accordion](../Document-Types/Elements/Accordion.md) | Element type | Create |
| 9 | [Main Content Block Grid](../Data-Types/Block-Grid/Main-Content-Block-Grid.md) | Block Grid data type | Create |
| 10 | [Article Listing Page Collection View](../Data-Types/Collection-View/Article-Listing-Page-Collection-View.md) | Collection view data type | Create |
| 11 | [Master](../Templates/Master.md) | Template | Create |
| 12 | [Article Page](../Templates/Master/ArticlePage.md) | Template | Create |
| 13 | [Article Listing Page](../Templates/Master/ArticleListingPage.md) | Template | Create |
| 14 | [Home Page](../Templates/Master/HomePage.md) | Template | Create |
| 15 | [Article Page](../Document-Types/ArticlePage.md) | Document type | Create |
| 16 | [Article Listing Page](../Document-Types/ArticleListingPage.md) | Document type | Create |
| 17 | [Home Page](../Document-Types/HomePage.md) | Document type | Create |

## Apply checklist

- [ ] 1. Create data type folder `Toggle` and data type `Toggle (default on)`
- [ ] 2. Create data type folder `Text Area` and data type `Meta Description Text Area`
- [ ] 3. Create document type folder `Compositions`, then composition `pageDetailsComposition`, then fix-up
- [ ] 4. Create composition `seoComposition`, then fix-up
- [ ] 5. Create document type folder `Elements`, then element `accordionItem`, move it into `Elements`, then fix-up
- [ ] 6. Create element `richText`, move it into `Elements`, then fix-up
- [ ] 7. Create data type folder `Block List` and data type `Accordion Items`
- [ ] 8. Create element `accordion`, move it into `Elements`, then fix-up
- [ ] 9. Create data type folder `Block Grid` and data type `Main Content Block Grid`
- [ ] 10. Create data type folder `Collection View` and data type `Article Listing Page Collection View`
- [ ] 11. Create template `master`
- [ ] 12. Create template `articlePage` under `master`
- [ ] 13. Create template `articleListingPage` under `master`
- [ ] 14. Create template `homePage` under `master`
- [ ] 15. Create document type `articlePage`, then fix-up (sorts, descriptions, template)
- [ ] 16. Create document type `articleListingPage`, then fix-up (template, collection view)
- [ ] 17. Create document type `homePage`, then fix-up (template, allowed at root)
- [ ] 18. Set allowed children on `articleListingPage`: `articlePage`, `articleListingPage`
- [ ] 19. Set allowed children on `homePage`: `articleListingPage`
- [ ] 20. Verify every requirements doc against the site and set each status line

## Apply log

—
