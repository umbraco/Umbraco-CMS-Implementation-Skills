using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace <Namespace>;

// Approach A entry point. Umbraco type-scans for IComposer and runs every one inside
// .AddComposers(), so no Program.cs change is needed.
//
// Optional: add [ComposeAfter(typeof(TheOtherComposer))] ONLY when this must run after a specific
// composer, e.g. one whose registration you override. Without it, the order among composers is
// unspecified.
public sealed class <Feature>Composer : IComposer
{
    public void Compose(IUmbracoBuilder builder) => builder.Add<Feature>();
}
