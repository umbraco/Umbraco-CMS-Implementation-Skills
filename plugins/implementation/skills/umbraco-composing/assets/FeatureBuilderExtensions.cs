using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Routing;

namespace <Namespace>;

// Everything <Feature> adds to Umbraco, in one place.
//
// Call builder.Add<Feature>() from exactly ONE entry point: <Feature>Composer (Approach A), or
// Program.cs after .AddComposers() (Approach B). Collections de-duplicate, but service
// registrations and async notification handlers do not — calling it from both makes those
// handlers run twice.
public static class <Feature>BuilderExtensions
{
    public static IUmbracoBuilder Add<Feature>(this IUmbracoBuilder builder)
    {
        // Optional: services. Choose the lifetime deliberately. To REPLACE a service Umbraco already
        // registered, use builder.Services.AddUnique<TService, TImplementation>(lifetime) from
        // Umbraco.Extensions instead of AddSingleton/AddScoped, which would add a second one.

        // Optional: a notification handler (swap in the notification you need). The async variant
        // suits anything doing I/O.
        builder.AddNotificationAsyncHandler<ContentPublishedNotification, <Feature>PublishedHandler>();

        // Optional: an item in an ordered collection, pinned to the core item it must precede.
        // Append() would put it after Umbraco's own finders, where a URL they match never reaches it.
        // The anchor must be registered or boot fails with InvalidOperationException: Umbraco 17's
        // URL finder is ContentFinderByUrlNew (ContentFinderByUrl is obsolete and not registered).
        builder.ContentFinders().InsertBefore<ContentFinderByUrlNew, <Feature>ContentFinder>();

        // Optional: startup/shutdown work, run only once Umbraco is installed and up to date.
        builder.Components().Append<<Feature>Component>();

        return builder;
    }
}
