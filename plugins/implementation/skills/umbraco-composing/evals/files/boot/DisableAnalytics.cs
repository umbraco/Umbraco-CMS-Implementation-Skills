// Eval 3's second boot: copy into MySite.Web/Infrastructure/ to switch the analytics feature off, as
// some of the user's deployments do. CampaignComposer must still boot, so its [ComposeAfter] on
// AnalyticsComposer has to be weak.
using Umbraco.Cms.Core.Composing;

[assembly: DisableComposer(typeof(Acme.Site.Infrastructure.AnalyticsComposer))]
