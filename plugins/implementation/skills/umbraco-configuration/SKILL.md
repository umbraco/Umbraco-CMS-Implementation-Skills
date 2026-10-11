---
name: umbraco-configuration
description: >
  Configure an Umbraco 17+ site for a target environment using golden, tested appsettings
  profiles: production single server, load balanced (SchedulingPublisher + Subscriber), and
  Azure App Service with Blob storage. Use this skill whenever the user asks to
  configure Umbraco, set up appsettings.json / appsettings.Production.json, prepare Umbraco for
  production, "go live", set the runtime mode, load balance or scale out Umbraco, host Umbraco on
  Azure or App Service, move media to Blob storage, set an Umbraco setting through an environment
  variable, or fix a BootFailedException about runtime mode, HTTPS or UmbracoApplicationUrl.
  SKIP: non-Umbraco projects, Umbraco < 17, Umbraco Cloud (its config is managed for you), and
  property-editor / data type configuration (that is content modelling, not appsettings).
---

# Umbraco configuration

Umbraco reads standard .NET configuration, so "configure Umbraco" means choosing the right
settings *for the environment* and getting them from the right *source*. This skill ships three
golden profiles that are validated against Umbraco's own options classes, its shipped JSON
schema, and its production runtime validators — start from one instead of assembling settings from
memory. General reference: [Configuration](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/configuration.md).

| Scenario | Reference | Assets | Pick it when |
|---|---|---|---|
| **Production, single server** | [production-single-server.md](references/production-single-server.md) | `production.json` | One IIS/Linux/container instance |
| **Load balanced** | [load-balanced.md](references/load-balanced.md) | `load-balanced.json` + a role overlay, `ConfiguredServerRoleComposer.cs` | Two or more instances behind a load balancer, on any host |
| **Azure App Service** | [azure-app-service.md](references/azure-app-service.md) | `azure-app-service.json`, `azure-app-service-settings.json` | App Service + Azure SQL (+ Blob media). Scale-out also needs the load-balanced reference |

### How to decide

Ask where it will run and how many instances serve traffic. If the answer is "Azure" and more
than one instance, use **both** the Azure and load-balanced references. If unsure, start with
production single server — it is the subset the others extend. A developer machine needs none of
them: a fresh install already runs in `BackofficeDevelopment` mode with no configuration, and a
production profile will not boot there.

Each profile is a starting point: copy it to the target `appsettings.<Environment>.json`, replace
the `<Placeholder>` tokens, then keep only what differs from Umbraco's defaults.

## Rules that apply to every profile

- **Secrets never go in a committed file.** Connection strings, SMTP passwords, unattended-install
  passwords, storage keys: user-secrets in development, environment variables or Key Vault
  references elsewhere. The profiles contain none, and the tests fail if one appears.
- **Environment variables use `__` for `:`**: `Umbraco:CMS:Global:UseHttps` is
  `Umbraco__CMS__Global__UseHttps`. Precedence, lowest to highest: `appsettings.json`,
  `appsettings.{Environment}.json`, user-secrets (Development only), environment variables,
  command line.
- **Verify every key against the project's own schema** — `appsettings-schema.Umbraco.Cms.json`
  ships in the project root and matches the installed Umbraco version. Do not write a key from
  memory or from newer docs; the docs describe the latest version, and settings arrive in minors
  (for example `Hosting:MachineIdentifier` exists from 17.6, not in 17.5).
- **The schema is not always right.** It is generated from property names, so a few sections are
  named differently from the keys Umbraco binds. Known cases on 17.5.3: `DistributedJobs` (schema
  says `DistributedJobSettings`) and `Core:Debug` (schema says `Debug`). When the docs and the
  schema disagree, trust the docs *and confirm against the installed package*, not against a newer
  Umbraco source checkout. A wrong section name is silently ignored — the setting just never
  applies — so check that the behaviour changed, not only that the app starts. Never edit the
  `appsettings-schema*.json` files to make a key validate: the build regenerates them. Use the
  bound key and tell the user their editor will not recognise it.
- **Production mode fails the boot on purpose** unless `UseHttps` is on, `UmbracoApplicationUrl` is
  set, `ModelsMode` is `Nothing` and the app is a Release build. Treat a `BootFailedException` here
  as the profile doing its job.

## Version compatibility

Targets **Umbraco 17+** (profiles validated against 17.5.3). Server roles use
`IServerRoleAccessor` / `SetServerRegistrar`, present throughout 17. Load balancing the backoffice
itself is a 17+ option and is described, not shipped, in the load-balanced reference.

## Validation

Assertions live in [`evals/evals.json`](evals/evals.json); run them with the
`umbraco-skill-evaluator` skill.

The profiles are checked against Umbraco's options classes, schema and production validators, not
booted in a load-balanced or Azure topology. When handing over, say "validated against Umbraco's
configuration model", never "tested on Azure" or "tested under load balancing".
