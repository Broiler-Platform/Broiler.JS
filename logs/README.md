# LogParser

Run the parser from the repository root:

```bash
dotnet run --project logs/LogParser -- logs/shard-1.json
dotnet run --project logs/LogParser -- logs/shard-1.json --output json
dotnet run --project logs/LogParser -- logs/shard-1.json --type System.NullReferenceException
dotnet run --project logs/LogParser -- logs/shard-1.json --type System.NullReferenceException --context "Broiler.JavaScript.BuiltIns.Array.JSArray.Filter"
dotnet run --project logs/LogParser -- logs/shard-1.json --message "property set"
dotnet run --project logs/LogParser -- logs --most-common-problem
dotnet run --project logs/LogParser -- logs --most-common-problem --output json
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
