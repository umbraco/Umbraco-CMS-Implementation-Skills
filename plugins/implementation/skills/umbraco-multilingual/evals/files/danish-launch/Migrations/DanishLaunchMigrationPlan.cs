using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Infrastructure.Packaging;

namespace Acme.Web.Migrations;

/// <summary>
/// Runs once per environment on startup and imports Migrations/danish-launch-package.xml
/// (the da-DK language and our ~40 dictionary items), so the Danish launch is repeatable.
/// Picked up automatically: Umbraco discovers PackageMigrationPlan types and runs them at boot.
/// </summary>
public class DanishLaunchMigrationPlan : PackageMigrationPlan
{
    public DanishLaunchMigrationPlan()
        : base("Acme Danish launch")
    {
    }

    protected override void DefinePlan()
        => To<DanishLaunchMigration>(new Guid("c2b7f4d1-5e9a-4a63-8d0f-3b1c6e7a9d02"));
}
