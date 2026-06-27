# LogParser

An internal Broiler.JS diagnostics tool. It is part of the unstable, AI-assisted first
preview and is covered by the repository's [pending human review](../HUMAN_REVIEW.md),
[Apache License 2.0](../LICENSE), and [Yantra JS provenance notice](../THIRD_PARTY_NOTICES.md).

Run the parser from the repository root:

```bash
dotnet run --project logs/LogParser -- logs/shard-1.json
dotnet run --project logs/LogParser -- logs/shard-1.json --output json
dotnet run --project logs/LogParser -- logs/shard-1.json --type System.NullReferenceException
dotnet run --project logs/LogParser -- logs/shard-1.json --type System.NullReferenceException --context "Broiler.JavaScript.BuiltIns.Array.JSArray.Filter"
dotnet run --project logs/LogParser -- logs/shard-1.json --message "property set"
dotnet run --project logs/LogParser -- logs --most-common-problem
dotnet run --project logs/LogParser -- logs --most-common-problem --output json
dotnet run --project logs/LogParser -- logs --highest-impact-problem
dotnet run --project logs/LogParser -- logs --highest-impact-problem --output json
```

The default text output now uses explicit sections and field names so each summary is easier to scan and post-process.

Before:

```text
File: shard-1.json
  Totals: declared executed=5928, passed=1933, failed=3995, skipped=0; parsed results=5928
```

After:

```text
File: shard-1.json
  Source:
    kind: file
    path: /abs/path/to/shard-1.json
  Metadata:
    suiteRef: ...
    broilerDll: ...
    bucketDepth: 4
  Totals:
    declaredExecuted: 5928
    passed: 1933
    failed: 3995
    skipped: 0
    parsedResults: 5928
```

JSON mode emits the same report shape in a machine-friendly format:

```json
{
  "outputFormat": "json",
  "summaries": [
    {
      "source": {
        "kind": "file",
        "name": "shard-1.json",
        "path": "/abs/path/to/shard-1.json"
      },
      "metadata": {
        "suiteRef": "...",
        "broilerDll": "...",
        "bucketDepth": 4
      },
      "totals": {
        "declaredExecuted": 5928,
        "passed": 1933,
        "failed": 3995,
        "skipped": 0,
        "parsedResults": 5928
      }
    }
  ]
}
```

Exception summaries now emit every parsed error entry in each matching type/context/message group rather than truncating the output to a small set of examples, and each parsed exception entry includes the stack-frame line number when it can be extracted.

When `--type`, `--context`, and/or `--message` filters are active, LogParser suppresses the normal summary report and emits only matching exceptions. Message filters use case-insensitive substring matching, so both partial and exact message searches work. This applies to both text and JSON output; filtered JSON emits a `matches` collection instead of `summaries`.

When `--most-common-problem` is active, LogParser analyzes the parsed entries, picks the most common exception type, then the most common context within that type, then the most common message within that type/context pair, and emits a focused report for that highest-occurred problem. The default text output is GitHub-issue-ready markdown that includes the exception type, representative line number, context, message, and up to 20 unique sample filenames/paths. This applies to both text and JSON output; the structured JSON report emits a single `problem` object.

When `--highest-impact-problem` (alias `--highest-impact`) is active, LogParser ranks failures by an *impact score* instead of raw frequency. The score is a deliberately simple proxy for how disruptive a failure cluster is to the engine's overall reliability:

```
impactScore(group) = sum(areaWeight(path) for path in group) * max(1, distinctPathBucketCount(group))
```

Selection criteria:

- **Area weight** (`LogSummaryBuilder.GetAreaWeight`) gives more credit to failures that hit core-language tests than failures that only hit legacy or annex areas. Current weights: `test/language/` → 3.0, `test/built-ins/` and `test/intl(402)?/` → 2.0, `test/harness/` and `test/staging/` → 1.5, `test/annexB/` and everything else → 1.0.
- **Breadth** uses the number of distinct path buckets (at the configured bucket depth) the failure cluster spans. A regression that hits many subareas of the suite gets a higher score than one concentrated in a single directory.
- The runner first picks the highest-scoring (type, context) pair and then, within it, the highest-scoring message. Ties fall back to raw count, then alphabetic ordering, so the selection is deterministic.

This prioritization surfaces the failures that block the most of the suite first, even when a noisier cluster of low-impact legacy failures has a higher raw count. The flag is mutually exclusive with `--type`/`--context`/`--message` filters and with `--most-common-problem`. The text report adds an `Impact score` line that includes the score, the distinct-bucket count, and the occurrence count; the JSON report exposes the same `impactScore` and `distinctPathBucketCount` fields on the `problem` object.
