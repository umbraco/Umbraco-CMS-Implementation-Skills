# Running these evals

Run all four evals in `evals.json` together, with and without the skill, using the
`umbraco-skill-evaluator` skill. Run them once on Haiku and once on Sonnet. A stronger model's
baseline already knows most Umbraco 17 security settings, so a single strong-model run shows
little difference between the two sides.

## Inputs

Evals 1, 2 and 4 are prompt-only: there is no project to build. Eval 3 (`prelaunch-audit`) reviews
the three files in `files/prelaunch-audit/`:

| File | Planted defects |
|---|---|
| `Program.cs` | `AddSession` after `builder.Build()`, header middleware after `UseUmbraco()`, a site-wide CSP with no backoffice nonce, `X-XSS-Protection`, `AllowAnyOrigin()` with `AllowCredentials()`, cleared `KnownProxies`/`KnownIPNetworks` |
| `appsettings.json` | Production mode with `UseHttps: false`, no `UmbracoApplicationUrl` and `ModelsMode: InMemoryAuto`; a committed SQL password and Delivery API key; a weak password length; no routing hardening |
| `Controllers/PartnerOrdersController.cs` | `Partners` in the member-type argument of `[UmbracoMemberAuthorize]`, and orders chosen by a `memberEmail` query parameter |

The defects are deliberate. Don't fix them in this folder.

Copy `files/prelaunch-audit/` into the run's inputs and replace the `evals/files/prelaunch-audit/`
path in the prompt with that copy, so the run never sees the skill folder.

## Run prompt

Each run is told:
- the task, as the eval's `prompt` (copied into a plain text file, without the expectations);
- that it may fetch public docs, and where a mirror is if `docs.umbraco.com` is unreachable
  (`https://raw.githubusercontent.com/umbraco/UmbracoDocs/main/17/umbraco-cms/<path>`);
- that no .NET SDK or project is available;
- to save its reply to `outputs/ANSWER.md`, any files it writes beside it, and a transcript to
  `transcript.md`.

Give the with-skill run a **snapshot** of the skill that holds only `SKILL.md` and `references/`,
not the skill folder itself. `evals/` holds the expectations, and a run that can reach them will
read them; an instruction not to read them is not enough. Tell the baseline run not to read the
repository (`plugins/`, `.claude/`, `AGENTS.md`), or it will find the skill by browsing.

## Grading notes

- Grade from `outputs/ANSWER.md` and the files beside it, not from the transcript's summary.
- Several expectations accept two correct designs. Grade either as a pass:
  - an inline script that keeps a nonce, or a hash or external file where the page is
    output-cached (eval 1);
  - `KnownProxies`/`KnownIPNetworks` set to the proxy's ranges, or cleared and paired with an
    origin lockdown such as the `AzureFrontDoor.Backend` service tag plus `X-Azure-FDID` (evals 3
    and 4);
  - `X-XSS-Protection` removed or set to `0` (eval 3).
- The build-honesty expectations can only check that a run doesn't claim a build or a live
  request, since none is possible here. They pass in both configurations, so they don't
  discriminate.
