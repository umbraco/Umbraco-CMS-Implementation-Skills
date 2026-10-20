// Compiled into the web project ONLY by `scripts/boot-check.mjs --probe`, at build time, through an
// MSBuild targets file passed on the command line. It is never copied into the project's source.
//
// Adds GET /__boot-check/probe, which reports what Umbraco actually registered once the site has
// booted: the main collections in their real order, and the notification handlers and services that
// come from the solution's own projects (NuGet packages are left out), with lifetimes and counts.
// Evals grade runtime expectations from this instead of reading the code.

// Explicit System usings, so it compiles whether or not the project enables ImplicitUsings.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;

namespace BootCheckProbe;

public sealed class BootCheckProbeComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Keep the collection itself, not a copy: anything registered after the composers (Program.cs
        // after .AddComposers()) is in it too by the time the probe is requested.
        builder.Services.AddSingleton(new BootCheckProbeRegistrations(builder.Services));
        builder.Services.AddTransient<IStartupFilter, BootCheckProbeStartupFilter>();
    }
}

public sealed class BootCheckProbeRegistrations(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;
}

public sealed class BootCheckProbeStartupFilter : IStartupFilter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(nextMiddleware => async context =>
        {
            if (context.Request.Path != "/__boot-check/probe")
            {
                await nextMiddleware(context);
                return;
            }

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(BootCheckProbeReport.Build(context.RequestServices), JsonOptions));
        });
        next(app);
    };
}

internal static class BootCheckProbeReport
{
    private static readonly Type[] HandlerInterfaces = [typeof(INotificationHandler<>), typeof(INotificationAsyncHandler<>)];

    public static object Build(IServiceProvider provider)
    {
        IServiceCollection services = provider.GetRequiredService<BootCheckProbeRegistrations>().Services;
        HashSet<string> projects = ProjectAssemblies();
        bool Own(Type? type) => type is not null
            && type.Namespace != typeof(BootCheckProbeReport).Namespace
            && projects.Contains(type.Assembly.GetName().Name ?? string.Empty);

        // Keyed descriptors throw on ImplementationType, so they are skipped before it is read.
        List<ServiceDescriptor> plain = services.Where(d => !d.IsKeyedService).ToList();
        bool IsHandler(ServiceDescriptor d) =>
            d.ServiceType.IsGenericType && HandlerInterfaces.Contains(d.ServiceType.GetGenericTypeDefinition());

        return new
        {
            runtimeLevel = provider.GetRequiredService<IRuntimeState>().Level.ToString(),
            projectAssemblies = projects.Order().ToList(),
            contentFinders = Names(provider.GetRequiredService<ContentFinderCollection>()),
            urlProviders = Names(provider.GetRequiredService<UrlProviderCollection>()),
            healthChecks = Names(provider.GetRequiredService<HealthCheckCollection>()).Order().ToList(),
            components = Names(provider.GetRequiredService<ComponentCollection>()),
            notificationHandlers = plain
                .Where(d => IsHandler(d) && Own(d.ImplementationType))
                .GroupBy(d => (
                    Notification: d.ServiceType.GetGenericArguments()[0].FullName,
                    Handler: d.ImplementationType!.FullName,
                    Async: d.ServiceType.GetGenericTypeDefinition() == typeof(INotificationAsyncHandler<>)))
                .Select(g => new { notification = g.Key.Notification, handler = g.Key.Handler, async = g.Key.Async, registrations = g.Count() })
                .ToList(),
            services = plain
                .Where(d => !IsHandler(d) && (Own(d.ServiceType) || Own(d.ImplementationType)))
                .GroupBy(d => d.ServiceType.FullName ?? d.ServiceType.Name)
                .Select(g => new
                {
                    service = g.Key,
                    // In registration order: constructor injection gets the LAST one.
                    registrations = g.Select(d => new { implementation = Implementation(d), lifetime = d.Lifetime.ToString() }).ToList(),
                })
                .ToList(),
        };
    }

    private static List<string> Names<T>(IEnumerable<T> items) where T : notnull =>
        items.Select(item => item.GetType().FullName ?? item.GetType().Name).ToList();

    private static string Implementation(ServiceDescriptor d) =>
        d.ImplementationType?.FullName
        ?? d.ImplementationInstance?.GetType().FullName
        ?? "(factory)";

    /// <summary>The solution's own projects: libraries the app's .deps.json lists as type "project".</summary>
    private static HashSet<string> ProjectAssemblies()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // APP_CONTEXT_DEPS_FILES isn't always populated (it is empty under `dotnet <dll>`), so the
        // entry assembly's own <name>.deps.json beside it is read as well.
        var depsFiles = (AppContext.GetData("APP_CONTEXT_DEPS_FILES") as string ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Append(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetEntryAssembly()?.GetName().Name}.deps.json"))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var file in depsFiles.Where(File.Exists))
        {
            using JsonDocument deps = JsonDocument.Parse(File.ReadAllText(file));
            if (!deps.RootElement.TryGetProperty("libraries", out JsonElement libraries))
            {
                continue;
            }

            foreach (JsonProperty library in libraries.EnumerateObject())
            {
                if (library.Value.TryGetProperty("type", out JsonElement type) && type.GetString() == "project")
                {
                    names.Add(library.Name.Split('/')[0]);
                }
            }
        }

        return names;
    }
}
