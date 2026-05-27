# Agent usage guide

This guide explains how Broiler.JS contributors should pick and apply the
[agent profiles](profiles.md), and how we measure whether agent assistance is
actually improving our workflow and code-model quality.

## When to invoke an agent

Use a Copilot or automated coding agent for tasks that match one of the
profiles in [`profiles.md`](profiles.md) and meet all of the following:

1. The task is well-scoped — a specific bug, a specific spec clause, a
   specific doc page.
2. There is, or can be, an automated test that proves the change.
3. The change respects the modular boundaries in
   [`../architecture/overview.md`](../architecture/overview.md).

If a task fails any of these tests, do the design work as a human first and
then delegate the mechanical implementation.

## How to invoke an agent

1. **Identify the profile.** Pick the most specific entry from the
   [profile index](profiles.md#profile-index). When the change spans multiple
   profiles, anchor on the one that owns the failing test or the largest diff.
2. **Reference the profile in the prompt.** Start the agent prompt with
   `Profile: <name>` so reviewers can see which guardrails apply, e.g.
   `Profile: builtins-author — add Array.prototype.findLast (ES2023, ...)`.
3. **State the acceptance criteria.** Name the new or failing test, the
   ECMAScript clause, or the doc section the change must update.
4. **Let the agent run the targeted command from the profile.** Avoid asking
   it to run `dotnet test Broiler.JS.slnx` unless the change actually spans
   multiple assemblies.

## Reviewing agent output

Reviewers should treat agent-authored changes the same as human contributions
and additionally check that:

- The PR description names the profile used.
- New tests live in the `*.Tests` project owned by that profile.
- No modular boundary was violated (core engine ↔ feature satellites).
- No build artifacts, cache directories, or external suite snapshots were
  committed.

## Measuring effectiveness

We track the following metrics each release cycle to evaluate whether the
profiles are improving workflow and coding-model quality. They are intended to
be cheap to compute from existing data (GitHub PRs, CI runs, and the
compliance dashboard) — no extra tooling is required.

| Metric                                   | How to measure                                                                                  | Healthy direction |
| ---------------------------------------- | ----------------------------------------------------------------------------------------------- | ----------------- |
| Targeted-test pass rate on first push    | Share of agent PRs whose profile-recommended `dotnet test` command passes on the first push.    | ↑                 |
| Review iterations per agent PR           | Number of review rounds before merge, per profile.                                              | ↓                 |
| Modularity violations caught in review   | Count of review comments that flag a core ↔ satellite boundary break, per profile.              | ↓                 |
| Compliance dashboard delta               | Net change in pass count on [`../compliance/dashboard.md`](../compliance/dashboard.md), per release. | ↑              |
| Mean time from issue → merged PR         | Calendar time from issue open to PR merge for tasks tagged with a profile.                      | ↓                 |
| Documentation drift                      | Number of merges that change build/test commands without updating `.github/copilot-instructions.md` and `docs/agents/`. | 0 |

When a metric regresses for two consecutive cycles, treat it as a signal to
revisit the affected profile — usually by tightening the *Must do* / *Must
not* lists in [`profiles.md`](profiles.md).

## Keeping profiles current

The agent profiles, repository-wide instructions, and metrics in this guide
are living documents:

- When a build/test command changes, update both
  [`.github/copilot-instructions.md`](../../.github/copilot-instructions.md)
  and the matching profile in the same PR.
- When a new assembly is added, extend the profile index so an agent can find
  it without manual coaching.
- When recurring review feedback appears on agent PRs, fold it into the
  relevant profile's *Must do* / *Must not* list rather than repeating it on
  every PR.
