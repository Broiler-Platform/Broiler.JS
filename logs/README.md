# LogParser

Run the parser from the repository root:

```bash
dotnet run --project logs/LogParser -- logs/shard-1.json
dotnet run --project logs/LogParser -- logs/shard-1.json --output json
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

Exception summaries now emit every parsed error entry in each matching type/context/message group rather than truncating the output to a small example set.
