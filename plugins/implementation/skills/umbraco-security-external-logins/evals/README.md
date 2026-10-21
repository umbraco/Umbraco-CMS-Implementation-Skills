# Running these evals

Run all three evals in `evals.json` with and without the skill, using the
`umbraco-skill-evaluator` skill. Run them on a small model (Haiku) as well as a mid-size one
(Sonnet): a strong model's baseline already knows much of ASP.NET Core authentication, and the
skill's value shows most where a model has to guess at Umbraco's half of it.

## Inputs

Eval 1 is prompt-only. Evals 2 and 3 read the files under `files/`:

| Folder | What it is |
|---|---|
| `google-members/` | A working backoffice Google login (scheme `Google`, callback `/signin-google`) and the Login partial made from Umbraco's snippet. Eval 2 must add a member login beside it without breaking it. |
| `okta-backoffice/` | A broken backoffice Okta setup with planted defects. The manifest's `forProviderName` lacks `Umbraco.` and `allowPublicAccess`. The OpenID Connect options class checks the unprefixed scheme name, and no name claim is mapped. The default group is `"Editors"`, a display name rather than an alias. `OnAutoLinking` grants `admin` to everyone, the client secret is committed, and `DenyLocalLogin` is on. |

The defects are deliberate. Don't fix them in this folder.

Copy each run's input folder into that run's workspace and replace the `evals/files/...` paths in
the prompt with the copy, so the run never sees the skill folder.

## Run prompt

Each run is told:

- the task, as the eval's `prompt`;
- that it may fetch public docs (`docs.umbraco.com` pages are available as Markdown by appending
  `.md`);
- to treat the .NET SDK as unavailable;
- to write its reply to `outputs/ANSWER.md`, the files it produces beside it, and a transcript to
  `transcript.md`.

Give the with-skill run a **snapshot** holding only `SKILL.md`, `references/` and `assets/`, not
the skill folder: `evals/` holds the expectations. Tell both runs not to read this repository, or
the baseline finds the skill by browsing.

## Grading notes

- Grade from `outputs/ANSWER.md` and the files beside it.
- Expectations that accept more than one design pass either:
  - eval 1: a single-tenant authority with contractors as guests, or a tenant allow-list;
    password login kept, or a described break-glass account; the OpenID Connect handler with
    `NameClaimType = "name"`, or the Microsoft Account handler;
  - eval 2: an `email_verified` check, or a reasoned explanation that Google verifies the
    addresses it asserts.
- The build-honesty expectations only check that a run doesn't claim a build or a live request.
  They pass in both configurations, so they don't discriminate.
