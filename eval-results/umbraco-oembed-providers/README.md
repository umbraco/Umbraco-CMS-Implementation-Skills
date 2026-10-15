# Eval results: umbraco-oembed-providers

These are the stored results of `umbraco-skill-evaluator` runs, kept so later versions of the skill can be compared
against them. This folder isn't shipped in any plugin and isn't read by the validator or the `dotnet test` gate.

Each run folder holds `response.md`, `transcript.md`, `grading.json` (Haiku grader) and `timing.json`. Each iteration
folder has `benchmark.json`/`.md` and `review.html`. All runs used **one run per configuration**, so a single-assertion
difference can be noise. Each run's `eval_metadata.json` holds the expectations it was graded against.

| Iteration | Date | Skill version measured | Evals | Configs | Result |
|---|---|---|---|---|---|
| 1 | 15-10-2026 | First version (branch `oembed-providers-skill`, uncommitted at run time) | 1–5 (eval 6 is a trigger check) | with / without | 100% vs 96%; tokens ≈ equal (+0.4k); **48s vs 66s**. Baselines researched the Umbraco source (`gh api`) and found the same docs bugs; the only miss was not anchoring the regex (eval 3) |

## Trigger / conflict check

`trigger/multi_trigger.py` registers every published skill's description side by side in a temporary project, runs
each query in `trigger/trigger-queries.json` three times with `claude -p`, and records which skill was invoked. Iteration 1
(7 skills on the branch): **30/30** queries reached the expected skill or none. This includes the content-modelling
negative (Rich Text Editor toolbar → none) and the templating, pitfalls and multilingual probes.

Run it from any folder: `python3 multi_trigger.py <repo-root> trigger-queries.json out.json` (needs the `claude` CLI,
Python 3.9+, 30 `claude -p` runs).

## Adding an iteration

Put the next run in `iteration-<N>/` with the same layout and add a row above (skill commit, what changed). Compare only
evals whose prompts and expectations are unchanged.
