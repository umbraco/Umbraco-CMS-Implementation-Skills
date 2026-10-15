// Other languages may be partial: any key missing here falls back to en.js.
export default {
    contentTypes: {
        article: 'Artikelside',
        article_desc: 'En tekstbaseret, artikelagtig side på sitet.',
    },
    tabs: {
        content: 'Sideindhold',
    },
    groups: {
        titles: 'Sidetitler',
    },
    properties: {
        title: 'Hovedtitel',
        title_desc: 'Dette er sidens hovedtitel.',
        title_message: 'Hovedtitlen er påkrævet for denne side.',
    },
    package: {
        dashboardLabel: 'Velkommen',
        itemCount: (count) => {
            count = parseInt(count, 10);
            if (count === 0) return 'Viser ingenting';
            if (count === 1) return 'Viser ét element';
            return `Viser ${count} elementer`;
        },
    },
};
