# Load balanced

## When to choose this

Two or more instances behind a load balancer — on IIS, Linux or containers. On Azure App Service
read [azure-app-service.md](azure-app-service.md) as well. A single instance needs only
[production-single-server.md](production-single-server.md); load-balancing settings on one server
add failure modes for no benefit.

## Building blocks

- [Load balancing overview](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/server-setup/load-balancing.md)
  — roles, election, Data Protection, distributed cache
- [Standalone file system](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/server-setup/load-balancing/file-system-replication.md)
  — what to exclude from replication, Examine directory factories
- [Advanced / explicit roles](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/server-setup/load-balancing/flexible-advanced.md)
- [Load balancing the backoffice](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/server-setup/load-balancing/load-balancing-backoffice.md)
- [SignalR in load balanced environments](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/server-setup/load-balancing/signalr-in-backoffice-load-balanced-environment.md)
- [Common pitfalls](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/server-setup/load-balancing/common-load-balancing-pitfalls.md)
- [Logging with load balancing](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/server-setup/load-balancing/logging.md)

Fetch the overview and pitfalls pages first; load balancing fails in ways the settings alone do
not explain.

## Decide the topology first

1. **Dedicated backoffice (default).** One backoffice server outside the load balancer; N
   front-end servers inside it. This is what the shipped files implement.
2. **Load balanced backoffice** (17+). Every server is a `SchedulingPublisher`. Needs sticky
   sessions or Azure SignalR Service, `LoadBalanceIsolatedCaches()`, a shared
   `Hosting:TemporaryFileUploadLocation`, and `IDistributedBackgroundJob` for custom jobs. Not
   shipped here: follow the backoffice page above and do not mix it with the default files.

"Backoffice server" below always means the one server holding the `SchedulingPublisher` role. The
role name describes two jobs: it is where editors work and publish, and it is the only server
that runs Umbraco's scheduled background jobs (scheduled publish and unpublish, content version
cleanup, health-check notifications and the like). Front-end servers hold the `Subscriber` role:
they serve the site, pick up published changes, and never run those jobs. Two servers both acting
as scheduler would run each job twice, which is why the role is pinned rather than left to
election.

## What is shipped and what is not

| Piece | Shipped as | Covered by |
|---|---|---|
| Common settings (`load-balanced.json`) and the two role overlays | JSON | strict-bound against Umbraco's options, schema-checked, production validators |
| Role pinning (`ConfiguredServerRoleComposer.cs`) | C# composer reading `Deployment:ServerRole` | compiled; behaviour unit-asserted |
| Shared media (Blob or `UmbracoMediaPhysicalRootPath`) | not shipped | documented only — see the Azure reference for Blob |
| Data Protection key ring and distributed cache (`AddDistributedSqlServerCache` etc.) | not shipped | documented only — Microsoft's docs are the source; **without a shared key ring you get validation and decryption errors, with no startup warning** |

Say plainly, when handing over, that the last two rows still need doing and are not proven.

## Steps

Assume the site is deployed by a pipeline, not by hand: **one build artefact goes to every
server**, and anything that differs per server is supplied by the pipeline or host as environment
variables. Do not create a separate `appsettings` file per server or tell the user to copy files
onto individual machines.

1. Add `assets/load-balanced.json` to the project as `appsettings.Production.json`, so it ships in
   the one artefact. Set `<UmbracoApplicationUrl>` to an address the *backoffice server can resolve
   and reach*, because its scheduled jobs call the site on it (itself, the internal load balancer
   or the public URL) — not a per-machine name.
2. Copy `assets/ConfiguredServerRoleComposer.cs` into the project, replacing `<Namespace>`. It
   pins the role from `Deployment:ServerRole` and fails the boot on a typo; unset leaves Umbraco's
   election in place.
3. Give the user the two role-specific values as environment variables for their pipeline or host
   to set per deployment target (pipeline variables, IIS app-pool environment, container
   environment). The overlay files in `assets/` record the same values as JSON; they are the
   tested source, not files to deploy:
   - backoffice server (`assets/load-balanced-scheduling-publisher.json`):
     `Deployment__ServerRole=SchedulingPublisher`,
     `Umbraco__CMS__Examine__LuceneDirectoryFactory=SyncedTempFileSystemDirectoryFactory`
   - each front end (`assets/load-balanced-subscriber.json`):
     `Deployment__ServerRole=Subscriber`,
     `Umbraco__CMS__Examine__LuceneDirectoryFactory=TempFileSystemDirectoryFactory`
4. Exclude `~/umbraco/Data/TEMP` from any file replication (the profile moves temp files to the
   machine's temp folder, but replication of the rest of `umbraco/` still needs this).
5. Move media to shared storage and add the Data Protection and distributed-cache setup using the
   overview page. Send logs somewhere central rather than per-server files.
6. Test with a load-balanced staging environment: publish on the backoffice server, confirm every
   front end updates, and read every server's log for warnings.

## Done

State the topology chosen, the environment variables each deployment target needs, and that shared
media, Data Protection and distributed cache are the user's remaining responsibilities.
