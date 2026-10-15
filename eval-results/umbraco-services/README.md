# Eval results: umbraco-services

These are the stored results of `umbraco-skill-evaluator` runs, kept so later versions of the skill can be compared
against them. This folder isn't shipped in any plugin and isn't read by the validator or the `dotnet test` gate.

Each run folder holds `response.md` (the answer), `transcript.md`, `grading.json` (Haiku grader) and
`timing.json`. Each iteration folder has `benchmark.json`/`.md` and `review.html` where it was generated. All
runs used **one run per configuration**, so differences of a single assertion can be noise.

| Iteration | Date | Skill version measured | Evals | Configs | Result |
|---|---|---|---|---|---|
| 1 | 15-10-2026 | First draft (before AI-oriented rewrite), uncommitted | 1, 2, 3, 4, 6 (original prompts) | with / without | 100% vs 93%; +10.3k tokens (+20%) with skill. Every with-skill run fetched the ~8k-token docs page |
| 2 | 15-10-2026 | AI-oriented rewrite, before the "open another asset/fetch when needed" wording fix | same as 1 | with only | 100%; 53.3k tokens avg (vs 61.2k in iteration 1; baseline 50.9k) → +2.5k (+5%). No docs fetched |
| 3 | 15-10-2026 | Commit `87b2a06` | 1, 2, 3, 6 rewritten as trap prompts | with / without | 100% vs 96%; +3.7k tokens. Baselines researched the source themselves (`gh api`) |
| 4 | 15-10-2026 | Adds `IPublishedContentCache` option (B) with tests, the "URLs need a context" rule (runtime-verified), and the corrected *Culture unpublish* section | same as 3 (same expectations) | with only | 100%; 56.8k tokens (vs 58.2k in iteration 3; baseline 54.5k); 72s (vs 87s). Eval 3 answered from the skill instead of researching (−8.4k tokens, −42s) |

Notes per iteration are in each `benchmark.json` (`notes`) where present.

Iteration 2's run instructions were neutral ("follow SKILL.md"). Iteration 1's told agents to read "the reference
files and assets it points to", so part of the token drop between 1 and 2 may come from that wording.

Expectations changed during iteration 3: three were widened to accept the injectable singleton
`IPublishedContentCache`, and eval 3 was corrected to the verified `UnpublishedCultures` facts. Each run's
`eval_metadata.json` holds the expectations it was graded against.

## Trigger / conflict check

`trigger/multi_trigger.py` registers every published skill's description side by side in a temporary project,
runs each query in `trigger/services-trigger-queries.json` three times with `claude -p`, and records which skill
was invoked. Results: `trigger/results-iteration-1.json` (first description) and `results-iteration-2.json` (shortened
description). Both got 30/30 on the expected skill, with 0 false triggers.

Run it from any folder: `python3 multi_trigger.py <repo-root> services-trigger-queries.json out.json`.
It needs the `claude` CLI and Python 3.9+, and spawns 30 `claude -p` runs.

## Adding an iteration

Put the next run in `iteration-<N>/` with the same layout. Record the skill commit and what changed in the table
above, then compare `benchmark.json` across iterations, using only evals whose prompts and expectations are unchanged.
