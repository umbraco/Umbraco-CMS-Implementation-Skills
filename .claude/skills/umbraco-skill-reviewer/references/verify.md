# Stage 3 — Verify

Run these in the PR worktree after your fixes (and after merging `main`, if you did). Report each as
run and passed, run and failed (with the output), or not run (and why). Never in between.

## Always

```bash
node scripts/validate-skills.mjs --check-index     # run --write-index first if the index is stale
node scripts/generate-examples.mjs --lint
node --test scripts/*.test.mjs
dotnet build Umbraco-CMS.Skills.sln
dotnet test Umbraco-CMS.Skills.TestHost/Umbraco-CMS.Skills.TestHost.csproj --no-build
dotnet test Umbraco-CMS.Skills.TestHost.Blank/Umbraco-CMS.Skills.TestHost.Blank.csproj --no-build
```

Check the gate actually ran the skill's own tests (`--list-tests`, grep for the skill's fixture
names): a passing total that doesn't include them proves nothing about this skill. If you changed an
asset, the gate is what proves the change compiles and serves; the generated example picks it up at
build time.

## Boot the eval fixtures, when the evals use a running site

If the skill's `evals/files/` holds a web project (`Sdk="Microsoft.NET.Sdk.Web"`) and its README has
runs build it, boot the fixture as shipped, and for each overlay the evals apply:

```bash
node scripts/boot-check.mjs <copy-of-fixture>/MySite.Web --probe probe.json --out boot.txt
```

- Boot a **copy** assembled the way `evals/README.md` describes (base project plus the eval's
  overlay, plus `Directory.Packages.props` if the README copies it), not the files in the repo.
- A fixture that doesn't boot clean is a finding even when the run's code would be fine. Common ones:
  no `ModelsMode` (Umbraco 17's default `InMemoryAuto` needs a package most fixtures don't have, and
  logs a Fatal while still answering 200), or a tracked table a fresh install doesn't create (seed
  it with `--seed-sql`, as umbraco-composing's evals do).
- If the README tells runs not to build or start the site, the evals are code-reading only: booting
  is optional, and a boot failure is worth a mention, not a fix.
- A class-library fixture has nothing to boot; `boot-check` says so.

To show a boot or probe expectation can discriminate, boot one deliberately broken control as well
(the trap the skill warns about, applied to the fixture). It should fail.

## When to run evals

Only when something is off. The red flags:

- the PR reports no eval results, or leaves a model out;
- with-skill scores below 100%, or the baseline matches or beats it;
- your fixes changed what the skill **teaches** (advice, a template, an asset), not just wording;
- you changed `evals.json` expectations or fixtures;
- the PR description's eval claims don't match the files (an expectation it mentions doesn't exist,
  a tier it claims isn't true).

Otherwise the author's reported results stand, and the summary says you didn't re-run them.

When you do run them, keep it cheap: with `umbraco-skill-evaluator`, run **with-skill only**, on the
evals your change touches, Haiku first (weak models expose a router that isn't followed). Run
baselines only if the PR has none.

- **Which evals a change touches.** An eval whose expectations or fixture you changed; every eval a
  changed reference or asset is used in; and **every** eval when you changed what `SKILL.md` tells
  the agent to open, since all of them go through it.
- **Follow the skill's own `evals/README.md`** for the fixture, the run prompt and the grading
  artefacts. They differ between skills (`build.txt` or `razor-build.txt`, `final-answer.md` or
  `answer.md`, a seeded boot or none), and the expectations name them.
- **Tell each run that the snapshot's `references/` and `assets/` are part of the skill.** A run told
  only "don't read anything else" decided the references were off limits, never opened them, and
  answered from memory.
- **Check every `grading.json` before you believe it**: the number of graded expectations equals the
  number in `evals.json`, and the summary matches the individual verdicts (a grader has reported
  9/9 over ten passes). Check anything the grader couldn't (a file left unchanged, for example)
  yourself, with a diff.
- **A single failed expectation is not yet a regression.** Read the run: if it comes from your
  harness, fix the harness and re-run; if it's behaviour, re-run that eval once and compare with
  the PR's reported results. Report both runs.

## Traps

- **Shell word splitting.** zsh doesn't split unquoted variables: `${seed:+--seed-sql $seed}` arrives
  as one argument. Write multi-step boot or eval loops as `bash` scripts, with arguments in arrays.
- **Two builds of one project at once** race. Parallel boots each need their own copy.
- **Never trust a pass without process evidence.** `boot-check` requires runtime level `Run` from the
  booted process itself. If you boot any other way, confirm what answered was what you started, and
  that nothing is left running afterwards (`pgrep -fl <Assembly>.dll`).
- **"No such X" after an exception you caused** is your harness, not the PR: a usage error, a wrong
  path, an argument split wrong. Rule that out before you report a finding.
