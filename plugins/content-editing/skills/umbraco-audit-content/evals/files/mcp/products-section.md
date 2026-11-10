# CMS Editor MCP read results: Products section

Hand-written fixture shaped after the documented CMS Editor MCP tool outputs (content,
content-health, site-structure and relationships modes) for a fictional Umbraco 17 site,
Example Co, language `en-US`. Treat it as what the connected server reports. Nothing has been
written to the site.

## Tree (report-site-tree-summary, report-deep-pages)

```
Home                                   /                                level 1
  Products                             /products/                       level 2
    Booking                            /products/booking/               level 3
    Reports                            /products/reports/               level 3
    API                                /products/api/                   level 3
    Mobile App                         /products/mobile-app/            level 3
    Archive                            /products/archive/               level 3
      2023                             /products/archive/2023/          level 4
        Integrations                   /products/archive/2023/integrations/   level 5
          Legacy Integrations          /products/archive/2023/integrations/legacy-integrations/   level 6
```

report-deep-pages (depth > 4): `Legacy Integrations` (level 6).

## SEO audit (audit-page-seo)

| Page | SEO title | Meta description | H1 in content | Heading flags | Images / missing alt | Words |
|---|---|---|---|---|---|---|
| Products | Products, Example Co | "Booking, reporting and an open API for clinics of every size." | 0 (template renders page name as H1) | none | 2 / 0 | 410 |
| Booking | Online booking for clinics, Example Co | (empty) | 0 (template) | H2 then H4 under "Calendar sync" | 4 / 0 | 820 |
| Reports | Products, Example Co | (empty) | 0 (template) | none | 3 / 2 (`reports-dashboard.png`, `export-dialog.png`) | 640 |
| API | Products, Example Co | "Connect Example Co to the tools your clinic already uses." | 0 (template) | none | 1 / 0 | 530 |
| Mobile App | Example Co mobile app for staff | "Manage bookings on the go, even without signal, with full offline mode." | 0 (template) | none | 5 / 1 (`divider-wave.svg`, marked decorative in media) | 390 |
| Legacy Integrations | Legacy integrations, Example Co | "Our integrations for 2023." | 0 (template) | none | 0 / 0 | 180 |

Note: every page has 0 H1 in content; the `contentPage` template renders the page name as the H1.

## Content audit (audit-page-content)

**Mobile App.** Meta description: "Manage bookings on the go, even without signal, with full
offline mode." Body text (excerpt): "See today's bookings, check patients in and send reminders from
your phone. The app needs an internet connection; offline support is on our roadmap."

**Booking.** Body text (excerpt): "Patients book in seconds from your website. Trusted by over
4,000 clinics across Europe. Calendar sync works with Google Calendar and Outlook." No source is
given on the page for the 4,000 clinics figure.

**Legacy Integrations.** Body text (excerpt): "New for 2023: connect Example Co to PracticeOne and
MediBook. These integrations will be retired on 31 December 2024."

## References (report-content-references)

| Page | Referenced by |
|---|---|
| Legacy Integrations | none |
| API | Products, Booking |
| Mobile App | Products |

Navigation: the main menu lists Products, Booking, Reports, API and Mobile App. Archive pages are
not in the menu.
