using Microsoft.AspNetCore.Builder;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;

namespace Umbraco.Skills.Examples.ComposingProgramCs;

// COMPILE-ONLY harness, NOT generated from the skill's assets/. It is Approach B's one
// Program.cs change, in the full chain the skill tells users to keep: the extension is called
// after .AddComposers(), so its registrations run after every composer, and before .Build().
public static class ExampleProgram
{
    public static void ConfigureUmbraco(WebApplicationBuilder builder)
        => builder.CreateUmbracoBuilder()
            .AddBackOffice()
            .AddWebsite()
            .AddDeliveryApi()
            .AddComposers()
            .AddComposingExample()
            .Build();
}
