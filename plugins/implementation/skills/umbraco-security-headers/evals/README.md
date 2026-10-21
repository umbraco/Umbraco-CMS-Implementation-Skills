# Running these evals

Run the three evals in `evals.json` with and without the skill, using `umbraco-skill-evaluator`,
once on Haiku and once on Sonnet. A strong model's baseline already knows generic CSP practice,
so the gap shows on the Umbraco-specific traps below.

## Inputs

Eval 2 is prompt-only. Evals 1 and 3 read a folder from `files/`:

| Folder | Contents |
|---|---|
| `marketing-site/` (eval 1) | A 17.5.3 site with a locked `Program.cs`, a GTM inline snippet, Google Fonts and website output caching on. |
| `headers-review/` (eval 3) | A contractor's headers code with planted defects (listed in eval 3's `expected_output`). |

The defects are deliberate. Don't fix them in this folder.

Copy `files/` into the run's workspace and replace `evals/files/` in each prompt with that copy, so
a run never sees the skill folder.

## Run prompt

Each run is told:

- the task, as the eval's `prompt` (in a plain text file, without the expectations);
- that it may fetch public docs;
- that there is no running site, and not to run `dotnet build` or start one;
- to save its reply as `outputs/ANSWER.md`, with any files it writes beside it, and a short
  transcript as `transcript.md`.

Give the with-skill run a **snapshot** holding only `SKILL.md`, `references/` and `assets/`. A run
that can reach `evals/` will read the expectations. Tell the baseline run not to read the
repository, or it will find the skill by browsing.

## Grading notes

- Grade from `outputs/ANSWER.md` and the files beside it.
- The discriminating facts are the ones checked at runtime rather than taken from the docs: the
  tag helper's assembly is `Umbraco.Web.Common`; the backoffice entry script carries no nonce,
  so `'strict-dynamic'` blanks it; and the output cache replays the header and HTML it stored.
- The build-honesty expectations only check that a run doesn't claim a build or a live request.
  They pass in both configurations, so they don't discriminate.
