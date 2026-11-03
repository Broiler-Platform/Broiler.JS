# The side-by-side tool (bundle sjl-14-001, milestone SJL-14.2)

The tool behind decision rule part C ([../../decision-rule.md](../../decision-rule.md)) and, at W1's first parent and
at W1 with the switch off, behind part B's identity of P and K. It runs every input of the parse corpus
(`conformance/parse/corpus.manifest`, bundle sjl-12-001) under each of its variants at four entry points, on one
thread, and writes one report row for each input, variant and entry point.

| Entry | What it runs | Tree dumped |
|---|---|---|
| P | The old parser alone: `FastParser` over a `FastTokenStream`, script or module (bundle sjl-12-001's P, by link) | yes |
| B | The bridge alone: `SharedSyntaxBridge.Parse` with `BridgeRequest.Script` or `BridgeRequest.Module` | yes |
| K | The whole compile with the compiler's test-only switch off (bundle sjl-12-001's K, by link) | no |
| K-on | The whole compile with `JSCompilationOptions.ParseThroughBridge` on (decision D43) | no |

B and K-on are found by reflection when the tool runs, as sjl-12-001's tool finds the for-loop pass, so the tool builds
at W1's first parent too; there both answer `absent`, and the report's `#% bridge` and `#% switch` lines say so. The
canonical dump, the manifest reader, the field escapes and P and K are sjl-12-001's sources, linked unchanged
(`side-by-side.csproj`), so P's and K's rows are that tool's rows.

## Running it

From the repository root, with the .NET SDK the repository pins, building outside the tree as sjl-12-001's tool
does:

```bash
dotnet build -c Release -nologo --artifacts-path <scratch>/art docs/evidence/sjl-14-001/tools/side-by-side/side-by-side.csproj
env -u LANG TZ=UTC dotnet <scratch>/art/bin/side-by-side/release/side-by-side.dll \
  --manifest conformance/parse/corpus.manifest --test262 <test262 checkout at the manifest's pin> \
  --out <report.tsv[.gz]> [--entries P,B,K,K-on] [--dump] [--text <dumps[.gz]>] [--only <id>,...] [--trace]
```

- `--entries` picks the entry points, in the order given; all four by default.
- `--dump` hashes P's and B's trees with sjl-12-001's canonical dump; `--text` also writes each dump, headed
  `=== <id> <variant> <entry>`, for reading a difference.
- `--only` runs the named manifest ids alone; `--trace` names each input on standard error as it starts.

The tool refuses to run unless `TZ` is `UTC` and `LANG`, every `LC_*` and every `BROILER_*` variable are unset. It parses
and compiles on one thread of 256 MB with `CompilationStack.SizeBytes` 0, as sjl-12-001's tool does. A whole run of the
corpus at all four entries with dumps takes about an hour on the development container.

## The report

A header of `#%` lines (format, manifest digest and pin, entries, the bridge's and the switch's status, stacks,
environment, runtime and assembly versions), the column line, one row per input, variant and entry, and `#% end` with
the row count:

| Column | Holds |
|---|---|
| `id`, `variant` | The manifest id and the variant (`strict`, `sloppy` or `module`) |
| `entry` | P, B, K or K-on |
| `outcome` | `accepted`, `refused` (a `SyntaxError` or `FastParseException`), `crashed` (any other exception) or `absent` |
| `type`, `message` | The exception's type and message, escaped as sjl-12-001's fields; `-` when accepted |
| `dump-sha256` | The canonical dump's SHA-256 for P and B with `--dump`; `-` otherwise |

The header's assembly lines carry each build's informational version, so two reports from two commits differ there;
part B's identity is of the rows.

## Comparing

`compare.py` (run with `python3 -I`) reads reports:

```bash
# Counts of input variants by the pair of outcomes at two entries, and up to 20 variants of one kind.
python3 -I compare.py report.tsv P B
python3 -I compare.py report.tsv K K-on refused/refused\ message-differs 20
# Part B: P's and K's rows at W1's first parent and at W1, switch off, byte for byte (exit 1 when not).
python3 -I compare.py --same base.tsv head.tsv P,K
```

A P and B pair whose dumps differ is read with `--text` and a line diff of the two dumps; sjl-12-001's dump names
every field, so the first differing line names the node, the field and the token.
