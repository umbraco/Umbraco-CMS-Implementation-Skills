using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using <Namespace>.Mapping;

namespace <Namespace>.Composing;

/// <summary>
/// Registers the mapper so controllers, view components and services can take it by constructor
/// injection. Picked up automatically by Umbraco's composer scan — no Program.cs change needed.
/// Scoped is the safe lifetime: everything it depends on is singleton or scoped.
/// </summary>
public sealed class NullSafeComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<NullSafeContentMapper>();
}
