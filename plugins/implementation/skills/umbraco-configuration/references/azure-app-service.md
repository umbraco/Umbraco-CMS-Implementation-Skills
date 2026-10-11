# Azure App Service

## When to choose this

Hosting on Azure App Service with Azure SQL, optionally with Azure Blob for media. If more than
one instance will run, or you want a separate backoffice app, also follow
[load-balanced.md](load-balanced.md) — scale-out needs far more than the settings here. For a
non-Azure single server use [production-single-server.md](production-single-server.md); this
profile is that one plus the Azure-specific settings.

## Building blocks

- [Running Umbraco on Azure Web Apps](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/server-setup/azure-web-apps.md)
- [Load balancing Azure Web Apps](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/server-setup/load-balancing/azure-web-apps.md)
- [Azure Blob Storage for media and the ImageSharp cache](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/filesystemproviders/azure-blob-storage.md)
- [Azure Key Vault](https://docs.umbraco.com/umbraco-cms/run-in-production/security/key-vault.md)

Fetch the Blob page before wiring storage: the package names, versions and `Program.cs` calls are
not in `assets/` and change between releases.

## What the Azure-specific settings do

| Setting | Why |
|---|---|
| `Global:MainDomLock` = `FileSystemMainDomLock` | The default semaphore lock is Windows-only and does not work across App Service instances |
| `Hosting:LocalTempStorageLocation` = `EnvironmentTemp` | Keeps Umbraco's temp files on the instance's fast local disk, not the remote file share |
| `Examine:LuceneDirectoryFactory` = `SyncedTempFileSystemDirectoryFactory` | Lucene must read from local disk; this syncs the remote index to it. On a scaled-out *front end* use `TempFileSystemDirectoryFactory` instead (load-balanced reference) |
| `Umbraco:Storage:AzureBlob:Media:ContainerName` | Package configuration, outside `Umbraco:CMS` — the schema does not cover it |

## Steps

1. Save `assets/azure-app-service.json` as `appsettings.Production.json`; replace
   `<UmbracoApplicationUrl>` and `<AzureBlobContainerName>`. Drop the `Storage` block if media stays
   on the app's file system.
2. In the App Service, **Environment variables → Advanced edit**, paste
   `assets/azure-app-service-settings.json` and replace its tokens. Everything secret or
   per-environment lives here:
   - connection strings are **Key Vault references** (`@Microsoft.KeyVault(SecretUri=...)`), never
     literal values, and the app's managed identity needs *get* access to the vault
   - `slotSetting: true` on secrets and `UmbracoApplicationUrl`, so a slot swap does not carry
     staging's database or URL into production
3. For Blob media: add `Umbraco.StorageProviders.AzureBlob` and
   `Umbraco.StorageProviders.AzureBlob.ImageSharp`, then add the two builder calls from the Blob
   page to `Program.cs`. Existing media must be copied into the container's `/media` folder — it is
   not migrated automatically.
4. Use at least an S2 Azure SQL tier. Keep the web app, SQL and storage in one region.
5. Do **not** enable *Run from package* (`WEBSITE_RUN_FROM_PACKAGE`): Umbraco needs a writable
   working directory and will not boot.
6. Slot swaps with `SqlMainDomLock` need a slot-specific `Global:MainDomKeyDiscriminator`; with the
   `FileSystemMainDomLock` used here they do not.

## What is and is not verified

The JSON profile and app-settings file are validated against Umbraco's configuration model and a
no-literal-secrets check. The Blob wiring (packages and `Program.cs`) is **documented only** — it
ships no code here and nothing exercises it. Do not report Azure as "tested".

## Done

List the app settings the user must create in the portal, the Key Vault secrets and access policy
needed, and the Blob and scale-out follow-ups. Show the Blob code changes in the reply itself — the
packages added and the exact `Program.cs` lines — rather than only saying the files were edited.
