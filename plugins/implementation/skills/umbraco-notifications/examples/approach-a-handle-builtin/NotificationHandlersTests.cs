using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Umbraco_CMS.Skills.TestHost;

/// <summary>
/// Deterministic runtime validation of the umbraco-notifications skill (Approach A).
/// Proves the shipped PublishingRuleHandler, compiled from the example project, registered by
/// its composer and loaded into the reference instance, actually cancels a publish that breaks
/// the configured rule, and lets a compliant one through.
///
/// The fixture creates its own 'article' node under Clean's 'articleList' (Blog) node, so it
/// needs no particular published content and leaves the tree as it found it.
/// </summary>
[TestFixture]
// Mutates the shared content tree. NUnit runs sequentially by default; this keeps that
// requirement explicit, as in SitemapCacheInvalidationTests.
[NonParallelizable]
public class NotificationHandlersTests
{
    private const string FixtureName = "Notification handler fixture";

    private static T Resolve<T>() where T : notnull =>
        ReferenceSiteFixture.Factory.Services.GetRequiredService<T>();

    /// <summary>Clean's 'articleList' node ("Blog"), found by Document Type alias, not key.</summary>
    private static IContent FindByContentTypeAlias(IContentService contentService, string alias) =>
        contentService.GetRootContent()
            .SelectMany(root => contentService.GetPagedDescendants(root.Id, 0, 100, out _))
            .Concat(contentService.GetRootContent())
            .First(c => c.ContentType.Alias == alias);

    [Test]
    public void Publishing_an_article_with_an_uppercase_title_is_cancelled()
    {
        IContentService contentService = Resolve<IContentService>();
        IContent blog = FindByContentTypeAlias(contentService, "articleList");

        IContent node = contentService.Create(FixtureName, blog.Id, "article");
        try
        {
            // 'article' has mandatory properties (articleDate, author), so seed the fixture node
            // with a real article's values: without them Publish fails validation and reports
            // FailedPublishContentInvalid before the handler's cancellation can be observed.
            IContent existing = FindByContentTypeAlias(contentService, "article");
            foreach (IProperty property in existing.Properties)
            {
                node.SetValue(property.Alias, property.GetValue());
            }

            node.SetValue("title", "SHOUTING HEADLINE");
            contentService.Save(node);

            PublishResult blocked = contentService.Publish(node, ["*"]);

            Assert.That(blocked.Result,
                Is.EqualTo(PublishResultType.FailedPublishCancelledByEvent),
                "the handler should cancel the publish when 'title' is entirely upper case");
            Assert.That(
                blocked.EventMessages?.GetAll().Select(m => m.Category),
                Has.Member("Publishing blocked"),
                "the EventMessage the handler attached should reach the publish result; "
                + "this is the text an editor would see in the backoffice");

            // The earlier Save is unaffected by the cancelled publish: correcting the property and
            // republishing must succeed. The node is reloaded because the cancelled publish left
            // the in-memory instance with PublishedState.Publishing, which Save rejects.
            node = contentService.GetById(node.Id)!;
            node.SetValue("title", "Quiet headline");
            contentService.Save(node);

            PublishResult allowed = contentService.Publish(node, ["*"]);

            Assert.That(allowed.Result, Is.EqualTo(PublishResultType.SuccessPublish),
                "a compliant publish must succeed; a handler that cancels everything is broken, "
                + "not strict");
        }
        finally
        {
            // Leave Clean as found: the fixture node is deleted permanently.
            if (node.HasIdentity)
            {
                contentService.Delete(node);
            }
        }
    }
}
