# Stage 3 — Verify

Run these from the PR worktree's root after your fixes (and after merging `main`, if you did); `scripts/`
below is the repo's, not this skill's. Report each as
run and passed, run and failed (with the output), or not run (and why). Never in between.

## Always

```bash
node scripts/validate-skills.mjs --check-index     # run --write-index first if the index is stale
node scripts/check-skill-links.mjs                 # relative links and #anchors under plugins/
node scripts/check-skill-frontmatter.mjs
node scripts/generate-examples.mjs --lint
node --test scripts/*.test.mjs
dotnet build Umbraco-CMS.Skills.sln
dotnet test Umbraco-CMS.Skills.TestHost/Umbraco-CMS.Skills.TestHost.csproj --no-build
dotnet test Umbraco-CMS.Skills.TestHost.Blank/Umbraco-CMS.Skills.TestHost.Blank.csproj --no-build
```

- **If the gate fails and the cause isn't obvious**, [`umbraco-reference-instance`](../../umbraco-reference-instance/SKILL.md)
  covers running and debugging it.
- **Run it in your fresh worktree; don't rely on the author's pass.** A new worktree has no
  `umbraco/Data`, so the hosts install from scratch, and some failures only show there (on #50 a
  `requires` precondition passed on the author's machine and failed on every fresh install).
- **Check the gate ran the skill's own tests** (`--list-tests`, grep for the skill's fixture names):
  a passing total that doesn't include them proves nothing about this skill.
- **Prove the tests can fail when it matters**: if you changed an asset or a test, or the gate is the
  skill's only runtime evidence, break the asset once, see the skill's tests go red, and revert.
  Otherwise the author's "shown to fail" claim stands.
- The `NU1902`/`NU1903` package-vulnerability warnings appear across the whole repo, from Umbraco's
  own dependency graph. They aren't findings, and don't upgrade packages for them.

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

- the PR reports no eval results, or leaves a model out (a benchmark that records its model as
  `<model-name>` counts as leaving it out);
- with-skill scores below 100%, or the baseline matches or beats it;
- your fixes changed what the skill **teaches** (advice, a template, an asset), not just wording;
- you changed `evals.json` expectations or fixtures;
- the PR description's eval claims don't match the files (an expectation it mentions doesn't exist,
  a tier it claims isn't true).

Otherwise the author's reported results stand, and the summary says you didn't re-run them. A
baseline that ties with-skill on a strong model (common on Sonnet) isn't a reason to re-run
with-skill, since that can't change the tie: note it for the human as an eval that may not
discriminate.

When you do run them, keep it cheap: with `umbraco-skill-evaluator`, run **with-skill only**, on the
evals your change touches, Haiku first (weak models expose a router that isn't followed). Run
baselines only if the PR has none, which doubles the runs on a first review. Use `evals.json` as it
is: don't stop to ask about the prompts, as the evaluator's step 1 suggests, since reviews run on
their own. With no baseline, ignore the benchmark's delta: it compares against nothing.

- **Which evals a change touches.** An eval whose expectations or fixture you changed; every eval a
  changed reference or asset is used in; and **every** eval when you changed what `SKILL.md`
  teaches or routes to. Adding only the "open the reference and start from its asset" line isn't a
  teaching change: it makes the agent read more, not differently, so on its own it doesn't call for
  evals the PR already reports as passing.
- **Skip should-not-trigger evals** (a prompt the skill should *not* handle): a with-skill run loads
  the skill by force, so it can't test triggering. Say you skipped them and why.
- **Follow the skill's own `evals/README.md`** for the fixture, the run prompt and the grading
  artefacts. They differ between skills (`build.txt` or `razor-build.txt`, `final-answer.md` or
  `answer.md`, a seeded boot or none), and the expectations name them. With no README and no
  fixture (`files: []`), the evals are code-reading: each run writes its reply to `outputs/answer.md`
  and is graded from that and any files it created.
- **What each run must be told:**
  - the snapshot's `references/` and `assets/` are part of the skill (a run told only "don't read
    anything else" decided they were off limits and answered from memory);
  - its answer and transcript files are **required inputs for grading**, not optional reports
    (subagents are told not to write report files, and some skipped them and still said "done"); use
    the file names the skill's README gives, exactly;
  - what it must not read: the skill's `evals/` and `examples/` (snapshot the skill without them),
    the Umbraco source, and the NuGet package cache (a run read Umbraco's XML docs from it).
- **Keep the eval workspace in your scratchpad**, never in the repo or the worktree, even if the
  skill's README says to put it at the repo root.
- **An interrupted run counts as not run.** Don't grade partial output; start it again or report it
  as not run.
- **Give each grader the exact path to write `grading.json` to.** Without it, graders have saved it in
  the eval's folder instead of the run's.
- **Check every `grading.json` before you believe it**: it parses (a grader has written invalid
  JSON), the number of graded expectations equals the number in `evals.json`, the texts match, and
  the summary matches the individual verdicts (graders have reported 9/9 over ten passes, and saved
  the file one folder too high). Check anything the grader couldn't (a file left unchanged, for example)
  yourself, with a diff.
- **A single failed expectation is not yet a regression.** Read the run, then:
  - **your harness caused it**: fix the harness and re-run;
  - **the skill has a real gap**: fix the skill and re-run that eval;
  - **noise**: re-run that eval once and compare with the PR's reported results;
  - **the expectation is stricter than the behaviour** (it fails the same way twice, for a reason
    unrelated to your changes): leave it for the human, with the evidence. If your own fix made it
    fail (for example, new advice the expectation didn't allow for), correct the expectation and
    re-grade the same run.

  Grade every re-run the same way as the first (a Haiku grader) and report both runs. A weak model
  that keeps skipping an explanation step the skill already asks for is worth reporting, not
  re-running.

## Traps

- **zsh isn't bash.** It doesn't split unquoted variables (`${seed:+--seed-sql $seed}` arrives as one
  argument, and `for a in "x 1"; do set -- $a` doesn't split either), and `echo =====` fails (`=` expansion). Write anything with loops
  or argument lists as a `bash` script, with arguments in arrays.
- **Two builds of one project at once** race. Parallel boots each need their own copy.
- **Parallel reviewers slow every build.** Run long build, break and rebuild chains in the background
  rather than against a foreground time limit.
- **Probing a booted site by hand** (when `boot-check` can't answer the question): start it on a port
  of your own (`ASPNETCORE_URLS=http://127.0.0.1:<port>`). `umbraco-reference-instance`'s
  `instance.sh boot` reuses whatever already answers on port 44372, which in a batch can be another
  reviewer's site.
- **Never trust a pass without process evidence.** `boot-check` requires runtime level `Run` from the
  booted process itself. If you boot any other way, confirm what answered was what you started, and
  that nothing is left running afterwards (`pgrep -fl <Assembly>.dll`).
- **"No such X" after an exception you caused** is your harness, not the PR: a usage error, a wrong
  path, an argument split wrong. Rule that out before you report a finding.
