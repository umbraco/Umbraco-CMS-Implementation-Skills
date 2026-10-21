// English is the fallback for every other language, so keep this the complete set.
export default {
    contentTypes: {
        article: 'Article page',
        article_desc: 'A textual, article-like page on the site.',
    },
    tabs: {
        content: 'Page content',
    },
    groups: {
        titles: 'Page titles',
    },
    properties: {
        title: 'Main title',
        title_desc: 'This is the main title of the page.',
        title_message: 'The main title is required for this page.',
    },
    package: {
        dashboardLabel: 'Welcome',
        itemCount: (count) => {
            count = parseInt(count, 10);
            if (count === 0) return 'Showing nothing';
            if (count === 1) return 'Showing one item';
            return `Showing ${count} items`;
        },
    },
};
