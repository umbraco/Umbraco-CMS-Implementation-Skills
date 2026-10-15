# Running these evals

Run all three evals in `evals.json` together, with and without the skill, using the
`umbraco-skill-evaluator` skill. Run them once on Haiku and once on Sonnet: a stronger model's
baseline often recalls the docs' 2FA sample well enough that one strong-model run shows little
difference between the two sides.

## Project fixture (every eval, both sides)

Each run gets its own sandbox:

```
<run>/outputs/
  Directory.Packages.props     # copy of the repo-root file, so the project restores and builds
  project/                     # the "user's project": the agent edits here, in place
```

To build `project/`:
1. Copy `files/project/` into it: a minimal Umbraco 17 site, root namespace `Acme.Web`, with the
   `GoogleAuthenticator` package already referenced and ModelsBuilder in `Nothing` mode.
2. Overlay the eval's own folder from its `files` list:

| Eval | Overlay | Starting state |
|---|---|---|
| 1 | `files/member-portal/` | A working backoffice provider (`EditorAuthenticator`) and its manifest, a hand-written member login partial with no 2FA step, and a My account template with a TODO |
| 2 | `files/editor-2fa/` | The docs' user sample as a departed colleague left it: provider name `GoogleAuthenticator`, issuer `testingOn15`, default code tolerance, and a manifest with a `//` comment, a mismatched `forProviderName` and a placeholder custom `element` |
| 3 | `files/shared-provider/` | One provider class registered on both identity builders under the same name (the site fails to boot with a duplicate-key error), a QR code drawn by a third-party URL, and the docs' member view, which lists every provider and disables 2FA without a code |

All three assembled projects build. Eval 3's fails at boot, which `dotnet build` does not show.

## Run prompt

Each run is told:
- where the project is, and to edit it in place;
- that it may run `dotnet build` and fetch public docs, but not run the site;
- to save its reply to `outputs/final-answer.md` and a transcript to `transcript.md`.

Give the with-skill run the path of a **snapshot** of the skill that contains only `SKILL.md`,
`references/` and `assets/`, kept outside this repository. `evals/` holds the expectations and
`examples/` holds the tests, and a run that can reach them will read them. Tell both runs not to
read this repository or any Umbraco source checkout.

## Grading notes

Grade code expectations from the files under `outputs/project/`, not from `final-answer.md` alone.
For the build-honesty expectation, build the edited project: if the run says it builds, it should.
