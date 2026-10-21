# Notifications evaluation

These three scenarios test implementation correctness, not adherence to a particular prose style
or a target benchmark delta. A strong baseline is allowed to pass every check.

## Paired runs

- Copy `files/NotificationEval.csproj` and `files/Contracts.cs` into a fresh project for each run.
  Both configurations receive identical fixtures, tools, model, effort and time limits.
- Give the with-skill run a snapshot of `SKILL.md`, `references/` and `assets/`. Do not expose
  `evals.json`, this rubric or other runs' outputs to either executor.
- Freeze prompts and expectations before launching the runs. Keep every run, including failures.
- Build each submitted project independently against Umbraco 17.5.3. A disclaimer does not
  substitute for a passing build. Grade behavioral expectations from the submitted code with
  evidence; do not describe that review as runtime proof.
- Report documentation/source reads, tokens and time separately from correctness. Looking up an
  API is not itself a correctness failure, and a fetch count is not a token-saving measurement.
- Use an ignored repository-root `umbraco-notifications-workspace/` for results, outside the
  published skill tree. Never overwrite a previous iteration or commit evaluation outputs.

## Interpretation

The cases cover mixed batches/property cultures, local versus distributed cache work, and async
vetoes with nested transaction boundaries. The skill's committed reference-host tests independently
prove its shipped assets run; they do not prove every generated answer from these evals runs.

One run per case measures task-to-task spread, not repeatability; rerun the same frozen cases to
estimate run-to-run variation.
