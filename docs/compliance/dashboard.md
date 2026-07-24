# Compliance dashboard

This dashboard describes the current checked-in evidence surface. Release totals belong
here only when they link to raw artifacts from an exact Broiler commit and exact upstream
suite revision.

## Current repository state

| Area | Current evidence | Source |
| --- | --- | --- |
| Repository tests | Required; no result is inferred from documentation | `dotnet test Broiler.JS.slnx` |
| Full test262 script host | Sharded runner and post-merge workflow are implemented | `.github/workflows/test262.yml` |
| Tracked failures | The generated path list is the current source of truth; documentation does not duplicate its changing count | `scripts/compliance/test262-failures.txt` |
| Negative metadata | Expected-error handling is implemented and opt-in with `--include-negative` | `scripts/compliance/run_test262.py` |
| `$262` host tests | Excluded until the required host hooks exist | runner audit JSON |
| `module` / `raw` | Excluded from the ordinary script host; separate modes are still required | runner audit JSON |
| Cross-engine scenarios | Repeatable Broiler/Node/engine262 comparison command exists | `scripts/compliance/engine-scenarios.json` |

The active failure clusters and host gaps are summarized in
[Known compliance gaps](known-gaps.md). The completion order and exit gates are in the
[repository roadmap](../roadmap.md).

## Running evidence

Follow [the compliance process](process.md). The minimum release commands are:

```powershell
dotnet test Broiler.JS.slnx
python scripts/compliance/audit_test262.py --suite-ref <sha> --manifest-glob "scripts/compliance/test262-*.txt"
python scripts/compliance/run_test262.py --suite-ref <sha> --all-script-host-verifiable --shard-count <n> --shard-index -1 --include-negative
python scripts/compliance/compare_engines.py --manifest scripts/compliance/engine-scenarios.json --engine262-bin <path>
```

Use CI for the supported platform/shard matrix and publish its merged JSON/log artifact.

## Release evidence

Populate one row per release candidate; do not carry forward totals from an older
commit.

| Broiler commit | test262 revision | Host modes | Passed | Failed | Timed out | Excluded | Raw artifact |
| --- | --- | --- | ---: | ---: | ---: | ---: | --- |
| _No current release-candidate run recorded_ | — | — | — | — | — | — | — |

## Comparative engine matrix

Record only scenarios executed from the same manifest with named engine versions.

| Broiler commit | Scenario manifest | Broiler | Node/V8 | engine262 | Raw artifact |
| --- | --- | ---: | ---: | ---: | --- |
| _No current release-candidate matrix recorded_ | `scripts/compliance/engine-scenarios.json` | — | — | — | — |

## Regression tracking

- The checked-in failure manifest is a queue, not an allowlist of acceptable release
  behavior.
- Every unexpected failure needs an owning layer, minimal regression, and issue or
  roadmap entry.
- Paths leave the manifest only after the focused reproduction and full affected shard
  pass.
- A dashboard row must never claim “100% compliant” unless every supported host mode is
  green and every excluded category is explicitly outside the product claim.
