# Stage 9's listing: the source changes from `ecmascript-v0.1.0-preview.11` to the last model commit

**Written:** 2026-11-28, at SJL-15.2 stage 9, from Broiler.JS `5ad84ae22a82743eb115e55aac40b6a05d15e263`
(Broiler-Platform/Broiler.JS#1153, parts F and G at stage 8's commit, records) and Broiler.VM
`d384611fdb48c47325deba52561e4ce9bc3c146b` (Broiler-Platform/Broiler.VM#234). **A record, not a model commit, not a run
of the rule and not a verdict.** It changes no path under `Broiler.ECMAScript/`. It lists; it classifies nothing. The
classification JSD-0083's decision D30 asks for is Broiler.VM's, at its pin commit.

Every listing below is a `git` output kept in [`raw/`](raw/), written by the commands in
[`commands.txt`](commands.txt); their digests are in the bundle's [`hashes.txt`](../hashes.txt).

## The range

- **From:** tag `ecmascript-v0.1.0-preview.11`, which peels to `e4b6b49ed290983e02525ca1a9318fd46517dc2c`
  ([`raw/ls-remote-tags.txt`](raw/ls-remote-tags.txt), read from the remote with `git ls-remote`). Broiler.VM pins the four packages of the stream it uses at that version: `Directory.Packages.props` at
  `d384611f`, lines 28 (`Broiler.ECMAScript.Algorithms`), 34 (`Broiler.ECMAScript.Frontend`), 40
  (`Broiler.ECMAScript.Intl`) and 48 (`Broiler.ECMAScript.Generation`), each `Version="0.1.0-preview.11"`, with the
  comment at lines 25 to 27 naming "Broiler.JS e4b6b49e, tag ecmascript-v0.1.0-preview.11". JSD-0088's decision D-pin
  says this family "moved to `0.1.0-preview.11` before SJL-13's step 2".
- **To:** `159fd811f74d0f43e7ed7327081267a96f8f1196` (Broiler-Platform/Broiler.JS#1151), stage 8's commit, the tenth and
  last model commit by decision D-js-stages. Nothing outside `docs/` changed from `159fd811` to `5ad84ae2`.
- **Between:** tag `ecmascript-v0.1.0-preview.12`, which peels to `2f012739cfc4d99e91b22d28a09941c49d8bd646`
  (Broiler-Platform/Broiler.JS#1118). Both tags' commits are first-parent ancestors of `159fd811`
  ([`raw/log-first-parent-all-e4b6b49e-159fd811.txt`](raw/log-first-parent-all-e4b6b49e-159fd811.txt)).
- **What nuget.org lists** (flat-container index, GET, read 2026-11-28): `Broiler.ECMAScript.Algorithms`
  `0.1.0-preview.1` to `0.1.0-preview.12`; `Broiler.ECMAScript.Frontend` and `Broiler.ECMAScript.Intl`
  `0.1.0-preview.9` to `0.1.0-preview.12`; `Broiler.ECMAScript.Generation` `0.1.0-preview.5` to `0.1.0-preview.12`
  ([`raw/nuget-flat-container-*.json`](raw/nuget-flat-container-broiler.ecmascript.frontend.json)). So preview.12 is
  published, and no `0.1.0-preview.13` is listed or tagged.

## The four packages, by merge

Each package is its folder, `Broiler.ECMAScript/Broiler.ECMAScript.<package>/` (with the trailing slash, so a test
project's folder is not in it).

| Package | First-parent merges from `e4b6b49e` to `159fd811` that change its folder | `e4b6b49e` to `2f012739` | `2f012739` to `159fd811` |
|---|---|---|---|
| `Algorithms` | none ([log](raw/log-first-parent-Algorithms-e4b6b49e-159fd811.txt), [diff](raw/stat-Algorithms-e4b6b49e-159fd811.txt)) | empty ([diff](raw/stat-Algorithms-e4b6b49e-2f012739.txt)) | empty ([diff](raw/stat-Algorithms-2f012739-159fd811.txt)) |
| `Frontend` | the ten model commits below ([log](raw/log-first-parent-Frontend-e4b6b49e-159fd811.txt), [diff](raw/stat-Frontend-e4b6b49e-159fd811.txt)) | empty ([diff](raw/stat-Frontend-e4b6b49e-2f012739.txt)) | the ten model commits ([diff](raw/stat-Frontend-2f012739-159fd811.txt)) |
| `Intl` | none ([log](raw/log-first-parent-Intl-e4b6b49e-159fd811.txt), [diff](raw/stat-Intl-e4b6b49e-159fd811.txt)) | empty ([diff](raw/stat-Intl-e4b6b49e-2f012739.txt)) | empty ([diff](raw/stat-Intl-2f012739-159fd811.txt)) |
| `Generation` | none ([log](raw/log-first-parent-Generation-e4b6b49e-159fd811.txt), [diff](raw/stat-Generation-e4b6b49e-159fd811.txt)) | empty ([diff](raw/stat-Generation-e4b6b49e-2f012739.txt)) | empty ([diff](raw/stat-Generation-2f012739-159fd811.txt)) |

**The Frontend's merges**, oldest first, each against its own first parent
([`raw/stat-Frontend-by-merge-e4b6b49e-159fd811.txt`](raw/stat-Frontend-by-merge-e4b6b49e-159fd811.txt); added and
modified files in [`raw/name-status-Frontend-e4b6b49e-159fd811.txt`](raw/name-status-Frontend-e4b6b49e-159fd811.txt)):

| Merge | Stage | Files of `Broiler.ECMAScript.Frontend/` it changes |
|---|---|---|
| `be6c4a5c` (#1132) | 1 | `DeclarationTables.cs`, `DeclarationsWalk.cs`, `ParseRequest.cs`, `PublicAPI.txt`, `SemanticModel.cs` (added), `SyntaxParser.cs` |
| `6c61879b` (#1135) | 2 | `DeclarationsWalk.cs`, `ParseGoal.cs`, `ParseRequest.cs`, `Rows.cs`, `SemanticModel.cs`, `SemanticRows.cs` (added), `SyntaxParser.cs` |
| `d87044c9` (#1137) | 3 | `DeclarationsWalk.cs`, `PublicAPI.txt`, `SemanticModel.cs`, `SemanticRows.cs` |
| `56272150` (#1139) | 4 | `DeclarationsWalk.cs`, `InheritedContext.cs`, `PublicAPI.txt`, `SemanticModel.cs`, `SemanticRows.cs` |
| `4d8aa93a` (#1141) | 5a | `DeclarationTables.cs`, `DeclarationsWalk.cs`, `PublicAPI.txt`, `SemanticModel.cs` |
| `84d84cc3` (#1143) | 5b | `DeclarationTables.cs`, `DeclarationsWalk.cs`, `ParseSwitches.cs`, `PublicAPI.txt`, `SemanticModel.cs` |
| `30af0dad` (#1145) | 5c | `CallerScope.cs` (added), `DeclarationTables.cs`, `DeclarationsWalk.cs`, `InheritedContext.cs`, `ParseSwitches.cs`, `PublicAPI.txt`, `SemanticModel.cs`, `SemanticRows.cs` |
| `2c48beb1` (#1147) | 6 | `DeclarationsWalk.cs`, `PublicAPI.txt`, `SemanticModel.cs`, `SemanticRows.cs` |
| `950bea8f` (#1149) | 7 | `DeclarationsWalk.cs`, `PublicAPI.txt`, `SemanticModel.cs` |
| `159fd811` (#1151) | 8 | `Broiler.ECMAScript.Frontend.csproj` (its comment and description), `SemanticModel.cs`, `SemanticRows.cs`, `SyntaxParser.cs` |

No other first-parent merge in the range changes the Frontend's folder, so the Frontend's diff from `e4b6b49e` to
`159fd811` is the ten model commits' alone; and it equals its diff from `2f012739` to `159fd811`.

**Taken again at `159fd811`**, the facts stage 8's brief gives at `d3e64b4d`: the diffs of `Algorithms`, `Intl` and
`Generation` from `e4b6b49e` are empty; nothing under the Frontend's folder changed from `e4b6b49e` to `2f012739`; so
the Frontend's diff is the model commits alone. Each holds at `159fd811`. From `e4b6b49e` to `2f012739` the whole of
`Broiler.ECMAScript/` changes in one test file only, `Broiler.ECMAScript.Conformance.Tests/LibraryRunTests.cs`, by
#1118 itself ([`raw/stat-ECMAScript-e4b6b49e-2f012739.txt`](raw/stat-ECMAScript-e4b6b49e-2f012739.txt)), and no build
input below changes ([`raw/stat-build-inputs-e4b6b49e-2f012739.txt`](raw/stat-build-inputs-e4b6b49e-2f012739.txt)).

## The build inputs outside the folders, labelled

**Labelled: whether any of these counts as a "source diff" under JSD-0083's decision D30 is Broiler.VM's to classify,
and is not decided here.** They are the files outside the four folders that MSBuild, the packing or the tests read and
that changed in the range, by first-parent merge, oldest first
([`raw/log-first-parent-build-inputs-e4b6b49e-159fd811.txt`](raw/log-first-parent-build-inputs-e4b6b49e-159fd811.txt),
[`raw/stat-build-inputs-by-merge-e4b6b49e-159fd811.txt`](raw/stat-build-inputs-by-merge-e4b6b49e-159fd811.txt)). Looked
at and unchanged in the range: `.editorconfig`, `NuGet.config`, `Directory.Build.targets`, `eng/Broiler.Packaging.props`,
`eng/pack.ps1`, `eng/resolve-preview-version.mjs`, `eng/verify-feed.ps1` and `.github/workflows/publish-shared.yml`. All
of them changed after `2f012739`, so the same list is what the next version carries beyond preview.12
([`raw/stat-build-inputs-2f012739-159fd811.txt`](raw/stat-build-inputs-2f012739-159fd811.txt)).

| Commit | File | What changed |
|---|---|---|
| `cdf5559d` ("Update dependencies", a commit on the first-parent line, not a merge) | root `Directory.Packages.props` | Version lines of `Broiler.Regex`, the Roslyn packages (`Microsoft.Net.Compilers.Toolset`, `Microsoft.CodeAnalysis.CSharp`, `Microsoft.CodeAnalysis.CSharp.Scripting`), `Microsoft.CodeAnalysis.BannedApiAnalyzers`, `coverlet.collector`, `Microsoft.NET.Test.Sdk` and `xunit.v3`. The four packages' own package reference, `Broiler.Unicode.Algorithms` (Frontend and Intl), keeps its version line |
| `b2b226cd` (#1126, SJL-11.5) | `.gitattributes` | `docs/evidence/sjl-11-001/inputs/** -text` added |
| `83a9bd82` (#1129, SJL-14's W2) | `Broiler.ECMAScript/Directory.Build.props` | `VersionSuffix` from `preview.8` to `preview.12`, with the comment that says why |
| `83a9bd82` (#1129) | root `Directory.Build.props` | `BJS0002` added to `NoWarn`, with its comment. The shared folder's `Directory.Build.props` says at its lines 7 and 8 that it "is the nearest Directory.Build.props for them, so MSBuild never reads the repository root's" |
| `83a9bd82` (#1129) | `.gitattributes` | the line-ending rule's path for `JSUnicodeTableData.g.cs` moves from `Broiler.JavaScript.Ast` to `Broiler.JavaScript.Expressions` |
| `be6c4a5c` (#1132, stage 1) | `Broiler.ECMAScript/Directory.Build.props` | a dated note in the suffix's comment (2026-11-24); the suffix is unchanged |
| `2259fd4a` (#1134) | `eng/run-tests.sh`, `.github/workflows/ci.yml`, `.github/workflows/shared-libraries.yml` | `--engine-only` in `eng/run-tests.sh`; the test steps' `VSTEST_CONNECTION_TIMEOUT` of 300 s and, on Windows, `-m:2` (the owner's answer of 2026-11-25 kept the wait) |

**Beyond the four folders and these inputs**, `Broiler.ECMAScript/` changes from `2f012739` to `159fd811` in its test
projects, the Native AOT probe, the runners, `docs/frontend/design.md`, `CHANGELOG.md`, `CODE-ASSURANCE.md`,
`HUMAN_REVIEW.md` and `hairs.manifest.json`, none of them package content
([`raw/stat-ECMAScript-2f012739-159fd811.txt`](raw/stat-ECMAScript-2f012739-159fd811.txt),
[`raw/log-first-parent-ECMAScript-2f012739-159fd811.txt`](raw/log-first-parent-ECMAScript-2f012739-159fd811.txt)).

## The version a dispatch would publish

`.github/workflows/publish-shared.yml` resolves the version with `node eng/resolve-preview-version.mjs` under
`PROJECT_SET: shared` and `VERSION_SUFFIX: ${{ inputs.version-suffix }}`, after a checkout with `fetch-depth: 0`
("Only X.Y.Z-preview.N is ever resolved"; the input `version-suffix`: "Optional preview.N (unused and at least the next
preview). Empty selects automatically."). The script reads the packable projects of `Broiler.ECMAScript.slnf` (the four
packages; every other project there sets `IsPackable` false), takes their shared `PackageVersion` as the floor, here
`0.1.0-preview.12` from `Broiler.ECMAScript/Directory.Build.props:37-38`, and adds to the numbers it counts as spent
every version nuget.org's flat container lists for those packages and every `ecmascript-v*` tag; the answer is one past
the highest spent number ("cumulative, and monotonic even across a feed that forgets"). With an empty input, on a
dispatch: `0.1.0-preview.12` is spent on nuget.org and by its tag, nothing higher is, so **the dispatch would publish
`0.1.0-preview.13`** for all four packages, and the workflow's step "Record the published version as a tag" tags it
`ecmascript-v0.1.0-preview.13`.

**The suffix need not move first.** The resolver raises the floor past every spent number, so a floor of preview.12
neither refuses nor picks a used version; an input naming `preview.13` would be accepted too, and one naming
`preview.12` or lower refused. The suffix's own comment says so: "Moving it publishes nothing"
(`Broiler.ECMAScript/Directory.Build.props:30`). After the publication the comment's "The suffix names the stream's last
published version, preview.12" no longer holds; moving the suffix is a separate change, the pending version-suffix
change, and the session's, not this record's.

**What it carries beyond preview.12**, if it is published from a commit whose four folders and build inputs are
`159fd811`'s: the Frontend's ten model commits, listed above, and the build inputs listed above; nothing in the folders of
`Algorithms`, `Intl` or `Generation`. A dispatch on `main` packs `main`'s head when it runs, which after this record
merges is a records-only merge whose tree outside `docs/` is `159fd811`'s, unless something else merges first.

## Q1 and JSD-0083's decision D30

**Q1**, as the session framed it: "Publish a Broiler.ECMAScript stream version carrying the semantic model, from stage
8's commit, for Broiler.VM's pin commit (D-pin)?" Decision D-js-packable has the owner asked "only after stage 8 and
stage 9's listing" ([`SharedLibraries.md`](../../../roadmap/SharedLibraries.md), "Shared static semantics (SJL-15)"). The
owner's answer of 2026-11-27, as the session's records of that day hold it, whole:

> Given (2026-11-27, in chat, unasked, after the session listed the owner's open decisions): "All publish-decisions can be done and run. So if a publish yes/no is the question, do the publish, wait for the nuget-package-availability and continue". Read as: wherever a record leaves a publication (yes/no) to the owner, the answer is yes; the session dispatches the repository's own publish workflow (never `test262.yml`), waits until the published version is restorable from nuget.org, and continues. It answers Q1 (JSD-0083's D30, the Frontend stream version carrying SJL-15.2's model, after stage 9's listing) as "publish"; D30's syntax-half check is still read as the records require. It supersedes the earlier session rule "never publish to nuget.org" for owner-reserved publish questions only.

So the owner's answer answers Q1 as **publish**. The session publishes after this listing merges, by a dispatch of
`publish-shared.yml` on `main`, and waits until nuget.org lists the version, `0.1.0-preview.13` by the section above.
This record dispatches nothing and publishes nothing.

**D30, quoted from Broiler.VM's `origin/main` `d384611f`**,
`src/Broiler.VM.Profile.JavaScript/docs/decisions/0083-the-shared-syntax-library-and-the-wide-path.md`, lines 1781 to
1792:

> **Decision D30 - publication and the stream.** The stream version carrying the frontend is **published with the
> owner's approval before step 1 merges**; a library not complete is not packable. Before asking, **for every
> `Broiler.ECMAScript.*` package this family pins and will re-pin** (`Algorithms`, and `Generation`, which its
> architecture tests use), the source diff between the pinned version and the new one is empty or wholly a milestone's
> listed in that milestone's cut-over record; `Algorithms`' is empty (decision D9). Admitting it re-pins every
> `Broiler.ECMAScript.*` package this family holds, the runtime's `Algorithms` included, in step 1's merge. **At SJL-15's
> re-pin the same check covers the frontend's syntax half**: its diff since the version step 2 adopted is empty, or each
> change is listed in SJL-15's cut-over record and classified by SJL-15's rule here as a correction by row. Broiler.JS
> packs the frontend for SJL-14's pristine consumer from its own commit; a Broiler.JS release that carries the bridge
> waits for a published version. The publication itself is the owner's own choice, not taken here. The reason: SJL-D-4
> (c); JSD-0082's decision D29's shape. One version serves every package of the stream, so a re-pin carries every
> package's changes into the window that adopts it.

**What this listing gives D30's check:** for `Algorithms`, `Intl` and `Generation`, which Broiler.VM pins and will
re-pin, the source diff of each package's folder from `0.1.0-preview.11` to `159fd811` is empty; for the frontend, its
diff since `0.1.0-preview.11` is the ten model commits, listed by merge with their files; and the build inputs outside
the folders that changed, labelled. **What stays Broiler.VM's:** whether those build inputs count as a "source diff";
listing the changes in SJL-15's cut-over record; their classification "by SJL-15's rule here as a correction by row", at
its pin commit; and whether the model commits' files are the "syntax half". D30's syntax-half check (JSD-0083, lines
1786 to 1788) asks that each change since the version step 2 adopted be listed in SJL-15's cut-over record and
classified by Broiler.VM's rule as a correction by row. No such record exists before the pin commit (D-pin). The model
commits touch syntax-half files and may not be corrections by row. Stage 9's listing is what exists before asking. This
record reads the check neither as met nor as unmeetable.

**Facts, as information:**
- From W2, `Broiler.JavaScript.Parser`'s package depends on `Broiler.ECMAScript.Frontend` at the shared folder's
  version, "which must be one a consumer can restore" (`Broiler.ECMAScript/Directory.Build.props:28-30`).
- Whether the source must be the library commit is **unknown**: Broiler.VM's pin commit admits "the version of the
  shared stream that carries the model, as the owner published it" (Broiler.VM bundle `sjl-15-001`'s
  `decision-rule-area.md`, lines 97 to 98 at `d384611f`), and the library commit is fixed only at SJL-15.5's merge.
- "If SJL-15.4 needs a later published version than SJL-15.3, a second pin commit of the same kind precedes its base"
  (the same file, lines 100 to 101).
- EP0017 and EP0018 are named, with their reason, as no row the model can report (decision D-js-library-readings); with
  parts F's and G's other readings, they are the session's readings under the owner's direction, flagged for the owner,
  not answers of the owner's. Whether the rule's "No run is taken before the base it names exists" admits parts F's and
  G's early runs at `159fd811` is the owner's, and is not decided. The E6 lines of check 3b differ from bundle
  `sjl-13-001`'s classification (the note of 2026-11-27 in the bundle's [`README.md`](../README.md)).

Nothing here is declared met or accepted, and no decision's text changes. Every SJL-15 clause is unmet, and none of it
is accepted.
