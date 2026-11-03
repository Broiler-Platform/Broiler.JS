# Bundle SJL-13-001's decision rule, revised a third time 2026-11-03: part D's workflow half read at the part D commit

**Written:** 2026-11-03, at stage SJL-13.2f of milestone SJL-13, **after** every part of the rule, as revised twice,
had been taken at the library commit `d4b330d8ed25aeee0e7d5fe1e4129f3dc480b318` and seen, **part D's run among them**
(the README's section "The rule's parts at the library commit, 2026-11-03", merged in
`90b5790dc73ef9d791efe1c5ec0c26ac6cf4eb65`, Broiler-Platform/Broiler.JS#1097): parts A, B, C, E, F, G and H met, part
D not met, so clause 3 not met; and before any run it judges: no part D commit exists yet, and no run of the shared
workflow at one has been taken. This file revises the second revision
([`decision-rule-revised-2026-11-02-second.md`](decision-rule-revised-2026-11-02-second.md), commit
`7aff49a2396223bd70172854f5684c30cf0eee07`), and through it the first revision and
[`decision-rule.md`](decision-rule.md), as the rule's section "Revisions" says a rule revised after any run it judges
has been seen is revised: a new dated file in this directory, quoting what it revises whole (the second revision, at
the end of this file, which quotes the first, which quotes the rule), saying that it was written afterwards. **It was
written after the library commit's part D run was seen and its outcome known, and the reading it gives part D was
chosen with that run in view; it is therefore weaker evidence for part D than the rule as written.** No earlier rule
file is edited, and the verdicts recorded at the library commit under the rule as revised twice stand as recorded,
part D's not met among them.

**Whose decision.** The owner's decision of 2026-11-03, taken as recommended under the owner's direction of
2026-10-18, "Please do all recommended decisions and skip waiting for review". The remedy the stage recommended, which
that decision adopts, reads: "(1) the shared workflow publishes the probe with JIT as well, runs that publication over
the vector corpus and compares its report with the Native AOT run's, line for line, with the same no-warning check;
(2) a third dated rule revision, written after the library commit's part D was seen and so weaker evidence for part D,
reads part D's workflow run at 'the part D commit': the first default-branch merge after the library commit whose
shared-libraries.yml publishes the probe with JIT, provided nothing the probe or the library builds from moves between
the library commit and it (git diff --name-only d4b330d8ed25aeee0e7d5fe1e4129f3dc480b318 <part D commit> touches no
path under Broiler.ECMAScript/, conformance/vectors/, nor the root build files that reach the probe), else NOT MET; the
local half stays the one already taken at the library commit; every other part, clause and the rest of the rule
unchanged; clause 3 read from C (at the library commit) and D as revised." This file is (2); (1) is the change to
[`.github/workflows/shared-libraries.yml`](../../../.github/workflows/shared-libraries.yml) that this file accompanies
(commit `062dbb27a8a8c148e6ae296bdf50f0c0d38186a3`, on the branch `sjl-13-2f-partd2`, before this file).
The direction changes no rule by itself (the README's closing paragraph); this file is the rule's revision.

Four points of this file go beyond the remedy's words. Reviews of the change on its branch, one of the rule's
integrity and one of the workflow's correctness, recommended them before anything was filed or merged, and they are
taken under the same direction: the part D commit is found by a stated test of the workflow file, where "publishes the
probe with JIT" left it to judgement; the condition holds the workflow file, every line of it but comments, the
concurrency group's line and what remedy (1) adds; it holds the build's configuration files by name at any depth and in
any letter case, not only the root files that reach the probe today; and a push to `main` has a concurrency group of
its own in the workflow, so that a later push does not cancel the part D commit's run. The second and third can only
turn part D to NOT MET where the remedy's words would read it MET, and the last only keeps the run that decides part
D from being cancelled. The first names the merge the remedy names when the change lands by a merge commit; when it
lands by a squash or a rebase, which makes no merge commit, it names the first merge commit after it whose workflow
file has the steps, and the condition is read there. **Owner:** the Broiler.JS maintainer. **Reviewer:** none
independent; one person holds every role (EX-30).

**What the library commit's run showed.** Part D's configuration asks for the probe "published with Native AOT and with
JIT ... by .github/workflows/shared-libraries.yml at the library commit, and the same locally on linux-x64". The
workflow at the library commit publishes the probe with Native AOT only: its job `native-aot-probe` runs
`dotnet publish ... -r <rid>`, and its step "Run the corpus under the JIT, and compare" runs `dotnet build` and the
built assembly, so that its JIT run is a build's output and no JIT publication was taken there. Everything else part D
names held: the workflow's run at the library commit
([run 41553360489](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/41553360489), `library-commit/probe/workflow/`)
succeeded, with no warning in either Native AOT publication and each job's Native AOT and JIT build reports the same
bytes; locally on linux-x64 both publications were taken (`dotnet publish -c Release -r linux-x64` and
`dotnet publish -c Release -p:PublishAot=false`, `library-commit/commands.txt`) with no warning
(`library-commit/probe/warnings.txt`), and the Native AOT image's, the JIT publication's and the JIT build's reports
are the same bytes (`library-commit/probe/sha256.txt`), with no failure. The workflow's half of the configuration
could not be taken as written at the library commit, and the rule names no other commit.

**What was known before the library commit.** The gap was not first seen in the library commit's run. It was recorded
before the library commit merged: in the README's section "The rule's parts at stage SJL-13.2f's head", part D's row,
by commit `c20898fd867319be62542e55d2941024167d97dd` on the stage's branch (an ancestor of the library commit), which
reads "at this head the JIT publication was not taken: the JIT run is the solution build's output, as the shared
workflow's is", and the workflow was not changed before the library commit merged. This file remedies a gap known
before its outcome was taken, after that outcome was seen, and is weaker evidence for that reason too.

## The decision: part D's workflow half at the part D commit

**What the rule says** (part D, which neither earlier revision changed):

```rule
evidence class: publish and run (not a measurement)
population: Broiler.ECMAScript.Probe at the library commit, which references the frontend and runs the syntax vectors
configuration: the probe published with Native AOT and with JIT, with Broiler.VM's root settings (TrimMode full, warnings as errors), by .github/workflows/shared-libraries.yml at the library commit, and the same locally on linux-x64; each output retained
MET if and only if both publications succeed with no trim, AOT or single-file warning, and the two runs' answers are identical, line for line, and pass.
NOT MET otherwise.
```

**Part D now reads:**

```rule
evidence class: publish and run (not a measurement)
population: Broiler.ECMAScript.Probe at the library commit, which references the frontend and runs the syntax vectors; and the same probe at the part D commit, built from the library commit's inputs (the condition below)
configuration: the probe published with Native AOT and with JIT, with Broiler.VM's root settings (TrimMode full, warnings as errors), by .github/workflows/shared-libraries.yml at the part D commit, in that workflow's run on the push of the part D commit to the default branch, both legs of its job native-aot-probe (linux-x64 and win-x64); and locally on linux-x64 at the library commit, as taken there (library-commit/probe/, library-commit/commands.txt's part D lines: dotnet publish -c Release -r linux-x64 and dotnet publish -c Release -p:PublishAot=false), not taken again; each output retained
condition: the part D commit exists; git diff --name-only d4b330d8ed25aeee0e7d5fe1e4129f3dc480b318 <the part D commit> names no path under Broiler.ECMAScript/, conformance/vectors/ or data/unicode/, not eng/Broiler.Packaging.props, and no path, at the repository root or in a folder at any depth, whose last component is, in any letter case, .gitattributes, .editorconfig, .globalconfig, Directory.Build.props, Directory.Build.targets, Directory.Build.rsp, Directory.Packages.props, NuGet.config or global.json; and every line that git diff -U0 d4b330d8ed25aeee0e7d5fe1e4129f3dc480b318 <the part D commit> -- .github/workflows/shared-libraries.yml removes or adds is a comment line (its first character other than a space is #), the line under concurrency: that sets group: (the library commit's removed, and "  group: shared-${{ github.workflow }}-${{ github.ref }}-${{ inputs.package-version }}${{ github.event_name == 'push' && format('-{0}', github.sha) || '' }}" added), an added line of the steps "Publish with the JIT" and "Run the corpus under the JIT publication, and compare" in job native-aot-probe, or one of the paths probe-publish-jit.log and probe-jit-published.tsv added to that job's step "Keep the logs"
MET if and only if the condition holds, and both publications succeed with no trim, AOT or single-file warning, and the two runs' answers are identical, line for line, and pass: in the workflow's run, in each of its two native-aot-probe jobs, the Native AOT publication and the JIT publication report no warning, and the Native AOT image's report and the JIT publication's are the same bytes, with no failure; and locally, as taken at the library commit, the same.
NOT MET otherwise: among other cases, when no merge is the part D commit, when the condition fails, when the default branch has no completed run of shared-libraries.yml on the push of the part D commit, or when that run's first attempt has a native-aot-probe job that does not succeed, whichever of its steps fails, the build's run "Run the corpus under the JIT, and compare" among them.
```

**The part D commit** is the first commit M of
`git rev-list --first-parent --merges --reverse d4b330d8ed25aeee0e7d5fe1e4129f3dc480b318..origin/main` (a merge into
the default branch, `main`, in its first-parent history after the library commit) whose
`git show M:.github/workflows/shared-libraries.yml` has, in job `native-aot-probe`, a step whose `run` runs
`dotnet publish Broiler.ECMAScript/Broiler.ECMAScript.Probe` with `-p:PublishAot=false`, a step whose `run` runs that
publication over `conformance/vectors`, and `cmp probe-aot.tsv` against that run's report. Nothing else decides which
commit it is: a merge counts whether or not it changes the file, so that a change landed by a squash or a rebase, which
makes no merge commit, is read at the first merge commit after it whose file has those steps, and the condition is
read at that commit, whatever else has moved by then counting against it. The change this file accompanies is to be
merged by a merge commit, whose merge is then expected to be the part D commit.

**Its run** is the run of `.github/workflows/shared-libraries.yml` ("Shared libraries") whose event is `push`, on
`main`, at the part D commit, found as the library commit's was
(`gh run list -R Broiler-Platform/Broiler.JS -w shared-libraries.yml -e push -c <the part D commit>`); its first attempt
decides, and every attempt is retained. A run cancelled before it completes, for any reason, is no completed run. With
the change this file accompanies a push has a concurrency group of its own, its commit's, so that no later push to
`main` and no dispatch cancels the run of the part D commit's push. It is retained under `part-d-commit/probe/workflow/`
as the library commit's run is under `library-commit/probe/workflow/`: its jobs and steps, every job's log, gzipped,
and its artifacts' publish logs and reports' SHA-256 (the artifact of each native-aot-probe job now holds
`probe-publish.log`, `probe-aot.tsv`, `probe-publish-jit.log`, `probe-jit-published.tsv` and `probe-jit.tsv`), with
the output of the condition's `git diff --name-only` and of its `git diff -U0` of the workflow file.

**Why those paths.** They are what the probe and the libraries it references build from, read from the repository at
the library commit. The folders and the one file held by path: `Broiler.ECMAScript/` (the probe, every shared library
it references, the folder's `Directory.Build.props`, which is the nearest for each of them and sets warnings as errors
and the trim and AOT analyzers, the runners the probe links, and the data homes whose tables it compiles in);
`conformance/vectors/` (the corpus it runs); `data/unicode/` (the Unicode pin and files the probe's runner reads at run
time from the repository root, `data/unicode/pins/unicode.pin`, to generate the syntax area's tables); and
`eng/Broiler.Packaging.props` (imported by `Broiler.ECMAScript/Directory.Build.props`: deterministic builds, Source
Link and the build's CI switch). The names held at any depth are those of the files that MSBuild, NuGet, the SDK, the
compiler or git reads from the folder of a project, a source or a path, or from a folder above it:
`Directory.Build.props` and `Directory.Build.targets` (MSBuild imports the nearest of each, searching up from each
project; the repository root's `Directory.Build.targets` is the nearest for every project under `Broiler.ECMAScript/`,
which has none of its own); `Directory.Build.rsp` (MSBuild reads a response file of that name from the folder of the
project it builds and from each folder above it, and adds its switches, a property among them, to the command line);
`Directory.Packages.props` (the central package versions, `Broiler.Unicode.Generation` and the libraries'
`Broiler.Unicode` packages among them); `NuGet.config` (the one feed they restore from; on Linux NuGet looks for
`nuget.config`, `NuGet.config` and `NuGet.Config` alike); `global.json` (none exists at the library commit; one added
would choose the SDK); `.editorconfig` (the compiler reads each one from a source's folder up to one with
`root = true`; at the library commit the repository root's, `root = true`, is the only one it reads for the probe's
sources, and one can change an analyzer's severity under warnings as errors); `.globalconfig` (the compiler's targets
in the SDK add a file of that name from every folder above each compiled source as a global analyzer configuration,
`Microsoft.Managed.Core.targets`, `GlobalAnalyzerConfigFiles`, with the same reach); and `.gitattributes` (git reads
one in every folder, a deeper one overriding the root's for the paths below it, so that a `conformance/.gitattributes`
or a `data/.gitattributes`, as much as the root's, would change the line endings with which the corpus or the Unicode
files are checked out, and so the bytes the Windows leg builds and the probe reads). They are held at any depth
because which of them reaches the probe depends on the folders they sit in, and in any letter case because the
Windows leg checks its files out on a file system that does not tell letter case apart. Holding a file that does not
reach the probe can only turn the part to NOT MET, never to MET: at the library commit the repository root's
`Directory.Build.props` does not reach the probe (the shared folder's own file is the nearest and imports only the
packaging file), and `Broiler.JS/Directory.Build.props` reaches only the projects under `Broiler.JS/`, and a change to
either between the library commit and the part D commit makes part D not met. What the condition does not hold: the
hosted runners' images and the SDK that `actions/setup-dotnet` installs for `10.0.x`, which are the platform's at each
run, as they were at the library commit's; each run's logs record them.

**Why the workflow's lines.** The workflow file holds both publications' commands, the setup of the SDK, the matrix of
both legs, the warning checks and the triggers. The condition admits only what the owner's remedy (1) adds (the JIT
publication's two steps and the two files its artifact keeps), comments, and the concurrency group's line; it admits
no removed line but a comment's or the group's, so that no step of the library commit's job, nor of any other job, nor
a trigger, can change. The workflow file's diff from the library commit to the change this file accompanies (commit
`062dbb27`) meets it.

**What the workflow's build run is.** The job keeps its run of the probe as `dotnet build` built it, with its `cmp`
against the Native AOT image's report (step "Run the corpus under the JIT, and compare"). That run is not part D's JIT
publication, and part D reads no report of it; but its step is one of the job's, and the job's success is what the NOT
MET line requires: a failure of that step in the first attempt makes part D not met.

## How clause 3 is read

The rule's line "A part run at any other commit, or whose vectors commit fails those conditions, is void." holds as
written for every part, but for part D's workflow half, which is run at the part D commit under the condition above;
part D's local half stays the run at the library commit. Clause 3, "Rule SL9 (determinism) and the Native AOT probe
pass.", is read from part C, as taken at the library commit, and part D as this file revises it; it is met when both
are MET. Until the part D commit's run exists, part D under this file is not taken, and clause 3 under this file is
not read; the verdicts the README records at the library commit under the rule as revised twice are not changed by
this file.

## What does not change

Everything else: the milestone and clauses judged; the rule commit, the vectors commit and the library commit, and
every other part run at the library commit from a clean checkout; parts A, B, C, E, F, G and H as the rule and its two
revisions read them, and their runs and verdicts at the library commit; decisions (a), (b), (c), (d), (e') and (f');
the classification, the catalogue, the stage table, the inputs and `readings/`; the readings of clauses 1 and 2; and
in part D its evidence class, its local half (taken at the library commit, not taken again), and the tests its MET
line names (no trim, AOT or single-file warning; the two runs' answers identical, line for line; and passing).

## The second revision, quoted whole

The text of [`decision-rule-revised-2026-11-02-second.md`](decision-rule-revised-2026-11-02-second.md) as commit
`7aff49a2396223bd70172854f5684c30cf0eee07` holds it and as this file is written (it quotes the first revision whole,
which quotes `decision-rule.md` whole in its turn):

``````markdown
# Bundle SJL-13-001's decision rule, revised a second time 2026-11-02: part A's limit reading tied to a measure of the source, the grammar pair read by a list, and corrections to the first revision

**Written:** 2026-11-02, at stage SJL-13.2f of milestone SJL-13, **after** the first revision of that day
([`decision-rule-revised-2026-11-02.md`](decision-rule-revised-2026-11-02.md), commit
`803371021cb75ea219127f0a0ae744f10f9a4fdd`) and after the runs taken under it at `a321cd7eed4652d72d931bd7cef41abd52ba35d7`
had been seen (the README's section "Parts A and C taken again under the revised rule"), and after a review of that
revision. None of those runs is a run of a part: the rule judges every part at the library commit, which does not exist
yet. This file revises the first revision, and through it [`decision-rule.md`](decision-rule.md), as the rule's section
"Revisions" says a rule revised after any run it judges has been seen is revised: a new dated file in this directory,
quoting what it revises whole (the first revision, at the end of this file, which itself quotes the rule whole), saying
that it was written afterwards, so that the decisions it changes are weaker evidence. Neither earlier file is edited.

**Whose decisions.** The two decisions below, (e') and (f'), are this stage's, made in answer to the review of the first
revision and adopted under the owner's direction of 2026-10-18, "Please do all recommended decisions and skip waiting
for review"; each is the fix the review recommended. Neither was among the decisions the stage's brief recommended. The
first revision recorded its decisions (e) and (f) as "the owner's decision of 2026-11-02, taken as recommended"; that
was true of (a) to (d), which the brief recommended, and not of (e) and (f), which were the stage's own proposals,
adopted under the same direction. This file withdraws (e) and (f) and puts in their place readings of the form the brief
recommended for the classification's errors and the comparison's equivalences: "read by a listed, mechanical mapping".
**Owner:** the Broiler.JS maintainer. **Reviewer:** none; one person holds every role (EX-30), and the review named here
changes no role.

**What does not change.** Everything the first revision leaves unchanged, and its decisions (a), (b), (c) and (d): the
provider's list (`readings/provider-readings.tsv`), the classification's readings
(`readings/classification-readings.tsv`), part C's control K2 as restated, and the reading of decision D14. The
classification is not edited. The corrections below change the first revision's words of explanation, not what any of
its decisions admits.

**Where the revisions are applied.** Part A's comparison, [`tools/compare_corpus.py`](tools/compare_corpus.py), is
revised in a commit after this file and before any run it judges. The two lists the decisions read are in
[`readings/`](readings/README.md), committed with this file, each written by a script committed with it whose `check`
mode rebuilds the list byte for byte: `limit-readings.tsv` by [`tools/limit_readings.py`](tools/limit_readings.py) and
`grammar-readings.tsv` by [`tools/grammar_readings.py`](tools/grammar_readings.py).

## (f') A limit's refusal at its bound, admitted only where the source is measured past it

**What the first revision says.** Its decision (f): "A reported depth equal to the configuration's nesting parameter
(with EX0011's code) or to the library's tree-depth constant (with EX0012's) is read as a depth past it: the library
reports that bound only when it refuses one past it. A reported depth below the bound is not admitted."

**What the review found.** The library's limit diagnostics always report the bound as their argument
(`Parser.EnterNesting` refuses with `maximumNesting`, `Parser.Charge` with `ParseLimits.MaximumTreeDepth`), so that a
depth equal to the bound is every depth the library ever reports, and a depth below it is never reported. Decision (f)
therefore admits every nesting or tree-depth refusal of an input the classification expects accepted, whatever that
input's real depth; its only ground is the library's own claim that it refuses only past the bound, which is what part
A's L was written to check. Stage 4f's fix round had removed the same admission from the comparison for that reason, and
its first finding was a within-bound over-refusal of just this kind, which (f) would have read as L. The six parses (f)
admitted are in fact past their bounds; what (f) lost is the test, not the outcome.

**Decision.** Decision (f) is withdrawn. Part A's L also admits an expected acceptance refused with EX0011's shared code
at a reported depth equal to the configuration's nesting parameter, or with EX0012's at a reported depth equal to the
tree-depth constant, **only** where `readings/limit-readings.tsv` lists that input, variant and configuration with that
limit; a refusal at a limit of any other expected acceptance is a failure. The list holds, and
`tools/limit_readings.py` writes, every input, variant and configuration that the classification expects accepted (not
semantics-only) and whose source, read from the manifest's input and checked against its SHA-256, the script measures
past the bound:
- **nesting**: the deepest nesting of brackets in the source, `(`, `[`, `{` and a template's `${`, each of which opens a
  production nested in the one around it, past the configuration's nesting parameter (64 in V, 512 in J);
- **tree**: the longest run of operands joined by one binary operator, the same token each time, of one left-associative
  precedence level (`+ - * % << >> >>> & | ^ && || ?? == != === !== < > <= >=`), each operand a single identifier,
  numeric or string literal token, all at one bracket depth, past the tree-depth constant (10,000). Such a run of n
  operators is a chain of n binary nodes, each the left operand of the next whatever binds the run's first or last
  operand, so that the tree holds a path of at least n chain nodes.

The script's lexer knows comments, string literals, template literals with their substitutions and regular-expression
literals, and nothing of the library; a source it cannot read whole, or whose brackets do not close in order, is
unmeasured and is not listed. Both measures are lower bounds of the depth they stand for: a listed line is an input that
nests past the bound by a count the library does not make, so that its refusal at the limit is L whatever the library
reports. As written today the list holds six lines, the six (f) admitted: J000243 (nesting 25,001) and J000799 (15,001),
past 512, and J000451 (a chain of 20,000 `+`), past 10,000, strict and sloppy, in J. Of the 2,839 inputs whose
brackets or operators are numerous enough to pass a bound, the script reads every one (none unmeasured), and no other
passes a bound.

**Why a measure of the source and not of the runner.** "The depth the library reports" is the library's diagnostic,
whose argument is its bound; a runner that reported something else would not report what the library does. The depth
the rule needs checked is the input's, so it is measured from the input, by a script that reads no output of the
library. **Why a list.** The list is written before any run it judges, from committed files alone, and a later run that
refuses another input at a limit is a failure until a further dated file lists it with its measure.

## (e') The two grammar rows read for each other by a list

**What the first revision says.** Its decision (e): "Where the classification names EX0008 or EX0009 among the rows
that may come first, the other's shared code is admitted first too."

**What the review found.** (e) is a general rule, wider than the 442 parses the triage read, where the stage's brief
asked for the classification's errors and the first-code check's strictness to be "read by a listed, mechanical
mapping". On its merits the rule is sound (both rows are the catalogue's generic grammar rows for one event, a token the
grammar does not admit where the parser stands, and which of the two a parser reports depends on its procedure, not on
the source), but it would admit a grammar refusal the triage never read.

**Decision.** Decision (e) is withdrawn. Part A's comparison admits EX0008's or EX0009's shared code first for the
other only for an input, variant and configuration `readings/grammar-readings.tsv` lists, reading G, with the row it
admits. The list holds, and `tools/grammar_readings.py` writes, every input, variant and configuration such that the
classification expects it refused and names exactly one of EX0008 and EX0009 among the rows that may come first, and
the run the triage read (`corpus/report.tsv.gz`, at `920f18f4579d729819bfcf3c02d18b75c78fd7b5`) refused it with the
other's shared code first. It holds 442 lines, the 442 parses of the triage's group (iii) and the same lines (e)
admitted in the run at `a321cd7e`. Reading G's ground for each is (e)'s: a token the grammar does not admit where the
parser stands (`using x` and `await using x` in a statement position, a getter's parameter, `import.source` with no
call, `yield ? …`, a class heritage that is an arrow, a case block's first statement, and their like).

## Corrections to the first revision's text

The first revision is not edited; these notes correct it, and change nothing any decision admits.
1. **Decision (a), condition 3.** Its justification, "Broiler.VM's refusal is one its validator gives", is false. Every
   provider row's `vm_code` in the catalogue is 2101 (32 of the 33 rows the stage table bases on the provider; EE0183
   has none), and 2101 is Broiler.VM's generic syntax code, among the `vm_code` of 125 of the catalogue's rows, EX0008, EE0047
   and EE0084 among them; condition 3 therefore means only that Broiler.VM's first code was 2101.
   It reads: in configuration V, a line is listed only if Broiler.VM's first code, as the classification's basis records
   it, is 2101, which excludes the 23 inputs refused in V with another first code. The evidence that the refusal is a
   regular-expression literal's is condition 2, Broiler.JS's scanner refusing a slash it read as a literal's start. The
   list is unchanged. (The review checked independently that every listed input and variant, 656 pairs covering the
   756 lines, is refused by another engine's parser, Node.js 22's `--check`, with "Invalid regular expression"; that
   check is not part of the reading.)
2. **Decision (a), last paragraph.** "beside the rows the classification names (decision (f))" points to the wrong
   place: the reading is the section "How part A's comparison reads the decisions".
3. **Decision (b), reading H3.** Its example gives J000040's text, `([a, b]) = [1, 2]`, for both inputs; J001147's is
   `({a, b}) = { a: 1, b: 2 }`. The reading holds of both: a parenthesised ArrayLiteral or ObjectLiteral is neither an
   ArrayLiteral nor an ObjectLiteral, so EE0032 does not apply and its AssignmentTargetType is invalid, EE0033.
4. **Whose decisions.** As said above, (e) and (f) were the stage's own proposals, not decisions the brief recommended.
5. **`readings/triage.tsv`'s labels** (that file is not edited; `readings/README.md` carries the note): "e: limit at the
   bound" is decision (f)'s outcome, now read by (f'); "a: provider reading P2" is decision (a)'s, and "P2" is defined
   nowhere; "iii: grammar pair EX0008/EX0009" is now read by (e').

## How part A's comparison reads the decisions, as revised twice

For each input, variant and configuration, in this order: an input listed `either` by decision (b) is read as
semantics-only; an expected acceptance refused at a limit is admitted as L when the reported depth is past the bound, as
the rule reads, or equal to it for a line decision (f') lists with that limit; an expected refusal is admitted when its
first code is one of the rows the classification names, or EE0019's where decision (a) lists it, or the row decision (b)
lists for it, or the row decision (e') lists for it. Every outcome so admitted is listed by its reason, with the reading
that admitted it (`L-nesting-measured`, `L-tree-measured`, `refused-by-provider-reading`,
`refused-by-classification-reading`, `refused-by-grammar-reading`, `semantics-only-read-…`); every other outcome is a
failure, as written. The comparison writes both new lists' SHA-256 to its summary.

## The first revision, quoted whole

The text of [`decision-rule-revised-2026-11-02.md`](decision-rule-revised-2026-11-02.md) as commit
`803371021cb75ea219127f0a0ae744f10f9a4fdd` holds it and as this file is written (it quotes `decision-rule.md` whole in
its turn):

`````markdown
# Bundle SJL-13-001's decision rule, revised 2026-11-02: part A's regular-expression provider, the classification's readings, two equivalences of part A's comparison, part C's control K2, and decision D14's residual shapes

**Written:** 2026-11-02, at stage SJL-13.2f of milestone SJL-13, **after** runs of parts A to H had been taken and seen at
the stage's branch heads (`920f18f4579d729819bfcf3c02d18b75c78fd7b5`, and the checks taken again at
`25678837cab780edf4cb166b40eacdf9c3e01b56`; the README's section "The rule's parts at stage SJL-13.2f's head"), none of
which is a run of a part: the rule judges every part at the library commit, which does not exist yet. This file revises
[`decision-rule.md`](decision-rule.md) as its section "Revisions" says a rule revised after any run it judges has been
seen is revised: a new dated file in this directory, quoting the rule whole (at the end of this file), saying that it
was written afterwards, so that the decisions it changes are weaker evidence. `decision-rule.md` is not edited.

**Whose decisions.** Each decision below is the owner's decision of 2026-11-02, taken as recommended under the owner's
direction of 2026-10-18, "Please do all recommended decisions and skip waiting for review", which JSD-0083 records its
decisions under. The direction changes no rule by itself (the README's closing paragraph); this file is the rule's
revision, and it is filed before any run it judges is taken: the runs at this branch's heads are void under the rule, as
every run before the library commit is. **Owner:** the Broiler.JS maintainer. **Reviewer:** none; one person holds every
role (EX-30), and nothing here has been read by anyone but its author.

**What does not change.** The milestone and clauses judged, the commits (the rule commit, the vectors commit and the
library commit, every part run at the library commit from a clean checkout, a part run elsewhere void), the
classification (`classification/classification.tsv.gz`, whose digest part A's completeness still pins to the rule
commit's), the catalogue, the stage table, the inputs, and parts B, D, E, F, G and H as written. Part A's population,
completeness, controls and its MET and NOT MET lines are as written; its configuration's provider clause, its comparison
and its admitted outcomes are revised by decisions (a), (b), (e) and (f) below. Part C is revised by decision (c) alone.
Decision (d) records how parts A and G read decision D14 as the library now implements it, and changes no text of theirs.

**Where the revisions are applied.** Part A's comparison, [`tools/compare_corpus.py`](tools/compare_corpus.py), and its
corpus runner, [`tools/frontend-corpus/`](tools/frontend-corpus/README.md), are revised in a commit after this file and
before any run it judges, as part A's comparison clause requires of the script; the lists decisions (a) and (b) read are
in [`readings/`](readings/), committed with this file: `provider-readings.tsv`, written by
[`tools/provider_readings.py`](tools/provider_readings.py), whose `check` mode rebuilds it byte for byte, and
`classification-readings.tsv`, with `triage.tsv`, the triage it lists. None of them holds an expectation the
classification does not: each names, for a listed input, variant and configuration, how the classification's own
expectation is read.

## (a) Part A's provider: a literal the classification refuses for a regular-expression reason, whatever row it names

**What the rule says.** Part A's configuration: "in configuration V and in configuration J the runner's
regular-expression provider answers as that configuration's engine validator answered the input in its SJL-12 after
baseline, as the classification records it (decision D26): it refuses a literal, naming the first row the
classification names that the stage table bases on the provider, exactly when the classification expects the input
refused in that configuration with such a row among the rows it names first, and admits every other literal".

**What the runs showed.** 710 parses the classification expects refused are refusals of a regular-expression literal
that the classification files under rows the stage table does not base on the provider, so that the provider, as
written, admits the literal and the library accepts: in configuration J, 255 test262 inputs (510 parses) whose first
rows are Broiler.JS's scanner codes alone, EX0001 or EX0008 (its scanner refuses a literal its validator refuses as a
token, and the parse-negative table took the scanner's site), while their basis names the provider rows the test262 file
is about; and in both configurations 50 inputs of Broiler.JS's corpus (200 parses), each a literal both engines'
validators refuse, whose first rows are every `syntax` row of Broiler.VM's first registry code, 2101 (the
classification's rule 2, which leaves the provider's rows out). The classification records the right outcome (refused)
and the wrong reason, so the provider, which answers from it, does not refuse.

**Decision (recommended: the mechanical reading).** The clause reads, with the words added in bold: "... it refuses a
literal, naming the first row the classification names that the stage table bases on the provider, exactly when the
classification expects the input refused in that configuration with such a row among the rows it names first, **and,
naming EE0019, exactly when the input, variant and configuration is one `readings/provider-readings.tsv` lists,** and
admits every other literal". The list holds, and `tools/provider_readings.py` writes, every input, variant and
configuration such that:
1. the classification expects it refused in that configuration, and names no row among those that may come first whose
   stage-table basis is `provider`;
2. Broiler.JS's SJL-12 after baseline, entry P (`docs/evidence/sjl-12-001/after/parse-corpus-c.tsv.gz`, an input the
   classification itself was built from), refused that input and variant with the message
   `Unexpected token Divide: / at <line>, <column>`: its scanner's answer when it read a slash as the start of a
   regular-expression literal and its validator, or the literal's delimiting, refused it (Broiler.JS's
   `FastScanner.ScanRegExp` returns no literal, and the slash stands as a division the parser does not admit);
3. in configuration V, the classification's basis also names Broiler.VM's first code (`VB:<code>`), and that code is the
   catalogue's `vm_code` of a provider row: Broiler.VM's refusal is one its validator gives.

"Refuses a literal", for both kinds, is read as the runner as committed reads it: the provider refuses the first literal
the library asks it about in that parse, and admits every other. EE0019 ("It is a Syntax Error if
IsValidRegularExpressionLiteral(RegularExpressionLiteral) is false") is the catalogue's row for a literal the provider
refuses whatever the pattern's fault, the row the library reports a refusal by when the verdict names no row of its own.
Part A's comparison admits, for a listed input, variant and configuration, EE0019's shared code first beside the rows the
classification names (decision (f)).

**Why this and not an explicit list of the 710.** Each condition is read from a file committed before the runs, and the
script reproduces the list, so the reading is the engines' recorded answer, not the triage's judgement; an explicit list
would be the triage's. The list holds 756 parses: the 710, and 46 in configuration J (23 test262 inputs, both variants)
whose literal is not delimited (`/\` at a line's end, `*/` after a stray `x`), which the library refuses before it asks
the provider anything, so that the reading cannot move them (the run under this revision shows each with the same first
code as without it). The third condition keeps configuration V to refusals Broiler.VM's validator code gives: without
it, 23 inputs refused in V for another reason would be listed.

## (b) The classification's errors, read by a listed mapping

**What the rule says.** Part A's "How it is read": "A failure is a library defect, fixed before the library commit, or a
classification error, which only a new dated rule file may correct, quoting this one."

**What the triage found** (`readings/triage.tsv`; the README's section "Part A's first codes, triaged"). Of the 1,180
refusals whose first code no row of the classification allowed, 112 were library sites reporting the wrong row, fixed in
the library with tests (commit `9d4c7ef323eb5184412379d15a0b087f4f2abe78`); 442 are decision (e)'s; 626 are
classification errors, where the library reports first the row whose condition holds and the classification names
rows that do not hold there; the 442 are refusals where the first-code check is stricter than an equivalent row allows,
which decision (e) reads by a rule rather than a list. Of the 26 refusals the library did not make, which stage 4f's records listed as library
defects, none is: in each the only rule whose condition holds is one the stage table assigns to `semantics`, which the
syntax half leaves to SJL-15's model (decision D10), and the row the classification names does not hold.

**Decision.** `readings/classification-readings.tsv` lists, for each of those 652 inputs, variants and configurations
(626 and 26), a reading and what it admits; part A's comparison admits, for a listed line, the listed row's shared code
first beside the rows the classification names, or, where it lists `either`, reads the line as an input the
classification marks semantics-only (admitted accepted or refused). The readings:

| Reading | What it admits | Inputs (parses) | Why the classification's rows do not hold and this row does |
|---|---|---|---|
| H1 | EX0001 | 48 test262 inputs (562) | A `#` followed by white space (`# x` in a class body or after `.`), or `#!` not at the start of the source: no input element of the lexical grammar begins there (PrivateIdentifier is `#` IdentifierName with nothing between; a HashbangComment stands only at the start, 12.5), which is EX0001. The classification named the engines' parser rows (EX0008, EX0009) and cited proposal rows, for a token their tokenizers make of `#` |
| H2 | EX0010 | 5 Broiler.JS inputs (20) | `import` or `export` in a source parsed with the Script goal: EX0010's own condition. Rule 2 took the `syntax` rows of Broiler.VM's code 2101, which do not hold EX0010 (whose code is 2401) |
| H3 | EE0033 | J000040, J001147 (8) | `([a, b]) = [1, 2]`: a parenthesised literal is neither an ObjectLiteral nor an ArrayLiteral, so EE0032 does not apply, and its AssignmentTargetType is invalid, EE0033 |
| H4 | EE0047 | J001200 (4) | `if (0) label: function f() {}`: IsLabelledFunction of the if statement's Statement, EE0047, in either code; EE0064 holds only in strict code |
| H5 | EE0155 | T030385 (4) | `async(x = await) => {}`: read under [+Await] the head covers no AsyncArrowHead (`await` with no operand), EE0155; the "Contains AwaitExpression" rows hold of no tree |
| H6 | EE0009 | T030370 (4) | `async(a = await => {}) => {}`: under [+Await] the head reads `await => {}` as an arrow whose BindingIdentifier is `await`, which EE0009 refuses; there is no AwaitExpression for the Contains rows |
| H7 | EX0008 | T039300, T042492, T043450 (12) | `async m(x = await) {}`: under [+Await] `await )` is an AwaitExpression with no operand, a grammar refusal at `)`; there is no AwaitExpression for the Contains rows |
| H8 | EX0008 | T029423, T029431 (8) | `({ default }) => {}`, `({ extends }) => {}`: a reserved word is no IdentifierReference, so the shorthand is a grammar refusal at `}`; EE0006 to EE0009 and EE0014 hold of identifiers |
| H9 | EX0008 | T001070 (4) | `for (a = 0 in {});`: neither a for-in head (no LeftHandSideExpression) nor Annex B's initialiser form (which needs `var`), a grammar refusal; EG0008 (a call as an assignment target) does not apply |
| S1 | `either` | T037840 to T037842, T048863, T048869, T049556, T049562 (22) | `for (this in …)`, `for ((this) of …)`, `for (import.meta in/of …)`: the rule that holds is EE0054 (a for-in/of head's AssignmentTargetType), which the stage table assigns to `semantics` (no parser checks it, the correspondence's F15); EG0008, the row the classification names, is about a call |
| S2 | `either` | T041636, T041638 (4) | `export { Number }`, `export { unresolvable }`: the rule that holds is EE0171 (an exported binding no declaration makes), `semantics`; EE0182 (a reserved word as an exported local name) does not apply |

**Why a listed mapping.** The classification stays as committed (its digest is part A's completeness); each listed line
names its reading, and each reading names the condition that holds and the one that does not, so that the list can be
checked line by line against the inputs. A reading a later triage finds wrong is corrected by another dated file.

## (c) Part C's control K2

**What the rule says.** Part C's controls: "K2 a side table filled from a HashSet<string>, whose order follows the
process's string-hash seed - run 1 against run 2 must differ, and rule SL9 must fail".

**What the runs showed.** Under K2's patch run 1 and run 2 differ (132,685 lines at `920f18f4`), but rule SL9 passes:
SL9's registered test compares two runs inside one process, where the string-hash seed is one, over the vector corpus,
whose `syntax` vectors read an outcome and no side table. No patch of K2's kind can make it fail; "rule SL9 must fail"
cannot hold of a correct control.

**Decision (recommended: restate K2's expectation to the cross-process comparison).** K2 reads: "K2 a side table filled
from a HashSet<string>, whose order follows the process's string-hash seed - run 1 against run 2 must differ, so that
this part's comparison of the runs' hashes, which runs in separate processes as the dumps are taken, reports the
difference". K1's text, both reverts, and part C's MET line ("both controls behave as stated and agree again after their
revert") are unchanged. **Why not run SL9 in two processes:** SL9 is a registered rule whose test part E judges as
registered (`rules.register.json`); running it in two processes would change the rule SL9 is, not the control, and the
dumps already run in three processes, which is where a seed-dependent order can show. The control's purpose, that part C
detects an order that follows the seed, is met by the comparison K2 now names.

## (d) Decision D14's residual shapes, as decided at stage 4f

**What the rule says.** Part A's L admits a refusal "with its tree-depth diagnostic (EX0012's shared code) at a reported
depth past the library's tree-depth constant (decision D14)"; part G's population names "the four left-operand shapes of
row R14".

**Decision (no text changes).** Both read decision D14 as the owner decided it on 2026-11-02 at stage 4f (the
derivation's section 5 as revised that day; Broiler.VM's JSD-0083 D14, by a dated note this stage hands Broiler.VM): the
library charges every chain node its height, one more than its highest child's, and refuses a tree deeper than
`ParseLimits.MaximumTreeDepth` wherever it is built, and no tree within it. Part A's L therefore admits a tree-depth
refusal of any shape past the constant, not of the four alone, as its text already reads; part G's population stays the
committed deep-nesting corpus (its four left-operand shapes among its rows), and the residual shapes are held by the
library's tests (`LimitsTests`), not by part G. In Broiler.VM's bundle of the same name, part A's C3, part B's L, part
C's fifth row and its control K4 name the four shapes alone; their revision is Broiler.VM's, by its own dated rule file
(the stage's report gives its text), and this rule reads none of them.

## (e) Two grammar rows read as one

**What the rule says.** Part A's admitted outcomes: "an expected refusal refused with a first code of the rows it names".

**What the triage found.** 442 refusals whose first code is EX0008's ("a token that no production admits where it
stands") where the classification names EX0009 ("a token that the production being parsed requires is missing") and not
EX0008, or the reverse, each at a token the grammar does not admit where the parser stands: `using x` and `await using x`
in a statement position, a getter's parameter, `import.source` with no call, `yield ? …`, a class heritage that is an
arrow, a case block's first statement, and their like.

**Decision.** Where the classification names EX0008 or EX0009 among the rows that may come first, the other's shared
code is admitted first too. **Why:** both are the catalogue's generic grammar rows for the same event, a token the
grammar does not admit where the parser stands; the specification gives a grammar failure no diagnostic of its own, and
which of the two a parser reports depends on whether its procedure expected one particular token there, a fact of its
strategy and not of the source. The reading admits nothing where the classification names neither (a grammar refusal
where it expects an early error stays a failure).

## (f) A limit's refusal reported at its bound

**What the rule says.** Part A's configuration records, "for a refusal at either limit, nesting or tree depth, the depth
the library reports", and its L admits a refusal "at a reported depth past the nesting parameter the configuration
sets, or ... past the library's tree-depth constant".

**What the runs showed.** The library's limit diagnostics report the bound refused past (their argument), not the depth
reached: it refuses a level of nesting, or a chain node's height, one past its bound
(`Parser.EnterNesting`: "nesting > maximumNesting"; `Parser.Charge`), so that a reported depth equals the bound for every
refusal at a limit, and as written L admits none. Six parses in configuration J (J000243 and J000799 refused at nesting
512, J000451 at tree depth 10,000, strict and sloppy) are refusals past the bound reported at it.

**Decision.** A reported depth equal to the configuration's nesting parameter (with EX0011's code) or to the library's
tree-depth constant (with EX0012's) is read as a depth past it: the library reports that bound only when it refuses one
past it. A reported depth below the bound is not admitted. **Why not change the runner:** "the depth the library
reports" is the library's diagnostic, whose argument is the bound (the design's section 9.2, "a limit's refusal has its
row's code, EX0011's or EX0012's, with its bound as its argument"); a runner that added one would report a depth the
library does not.

## How part A's comparison reads the decisions

For each input, variant and configuration, in this order: an input listed `either` by decision (b) is read as
semantics-only; an expected refusal is admitted when its first code is one of the rows the classification names, or the
row decision (b) lists for it, or EE0019's where decision (a) lists it, or, where the classification names EX0008 or
EX0009, the other's (decision (e)); an expected acceptance refused at a limit is admitted as L when the reported depth
is past, or equal to, the bound (decision (f)). Every outcome so admitted is listed by its reason, as part A's admitted
outcomes are, with the reading that admitted it; every other outcome is a failure, as written.

## The rule, quoted whole

The text of [`decision-rule.md`](decision-rule.md) as the rule commit (`238069ab8150858a112461b42b95c1f49c1f4eb1`)
holds it and as this file is written:

````markdown
# Bundle SJL-13-001's decision rule: the shared syntax library in its home, judged at library level by the parse corpus and test262's syntax tests as classified, by the syntax vectors, by determinism, by the Native AOT probe, by the shared libraries' rules, by its limits, by fuzzing and by the contract's checklist

**Written:** 2026-10-26, at milestone SJL-13 (work package SJL-13.1) of the shared JavaScript libraries programme,
with the frontend's area verdict this repository's `docs/roadmap/SharedLibraries.md` records ("Area verdicts") and
Broiler.VM's JSD-0083, before any code of the library exists: no `Broiler.ECMAScript.Frontend` project, no syntax
vector and no runner adapter. Committed in the same change: the classification
([`classification/`](classification/README.md)), built by `scripts/frontend/classify_parse_corpus.py` from the stage
table `Broiler.ECMAScript/docs/frontend/stages.tsv`, and part F's seeds and part G's deep-nesting corpus
([`inputs/`](inputs/README.md)). **Milestone judged:** SJL-13,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-13-shared-syntax-and-vm.md`: its home clauses 1 ("The
frontend's syntax half is implemented under the contract and the conventions."), 2 ("The parse corpus and test262's
syntax tests pass at library level, as classified.") and 3 ("Rule SL9 (determinism) and the Native AOT probe pass.").
**Owner:** the Broiler.JS maintainer. **Reviewer:** none. Every role this rule names is held by one person (the
programme records this as Broiler.VM's exception EX-30), and this rule does not claim that anyone independent has read
it.

**What this file is.** Eight predeclared decisions, one per part. Part B is template T3. Part A is an acceptance
comparison against a classification fixed before the library exists; part C is layer L13 and rule SL9; part D the
Native AOT probe (layer L10); part E the rule runs of SL1 to SL10; part F layer L9; part G the contract's limits; part H
a record-consistency check of the library against the contract. Each part decides alone.

**Which part reads which clause.** Clause 1 ("implemented under the contract and the conventions") is read from part H
(the contract) and part E (the conventions, through SL1 to SL10), with part G for the contract's limits. Clause 2 is
read from parts A and B. Clause 3 is read from parts C (SL9) and D (the probe). A clause is met when every part it is
read from is MET. **There is no base**: the library is new, and every part is judged at one commit. **There is no
measurement part**: the home clauses name none. It holds no figure.

**The syntax half** (decision D10) is the grammar and every early error either engine's parser reports at parse time
at the heads the correspondence names, the engine-only rows of kind Grammar and the contract's two limits among them;
the stage table assigns every catalogue row to it (`syntax`), to a switch of the request the library applies
(`request`: top-level `await`, which the library applies by reading a top level whose switch is off as a non-async
function's body, so that the row has no diagnostic of its own), to the engine that keeps it (`engine`: every other
engine-only Policy or Limit row, an import attribute the host cannot honour among them, which the library never
reports) or to SJL-15's semantic model
(`semantics`). **"As classified"** (clause 2, and SJL-13.2's "as both engines' baselines classify it")
is the classification: for every input of the parse corpus, under each of its variants and each configuration below,
the expected outcome at the syntax half, the catalogue rows whose shared codes may come first when it is refused, and
the reason - the test262 file's metadata, a `semantics` row that the syntax half leaves to the model, an engine
baseline, or a correspondence row that corrects one engine.

**The two configurations** (decision D26). **V**: every proposal flag off, no lexer extension, Annex B on for scripts,
HTML-like comments by goal, the nesting parameter at Broiler.VM's default, top-level `await` on for the Module goal
as Broiler.VM's module options set it and never set for the Script goal - Broiler.VM's request. **J**: the proposal
flags of the forms Broiler.JS parses today on, one flag per form (decorators, auto-accessors and the import phases'
call forms `import.source(…)` and `import.defer(…)`; the import phases' declaration forms off: decision D17), the
`m` extension, the nesting parameter at the library's supported maximum, top-level `await` on for the Module goal and
off for the Script goal, as Broiler.JS's module and script options set them (the switch applies to whichever goal a
request sets it for: decision D10) - Broiler.JS's request. In both, the regular-expression provider answers as that
configuration's engine validator answered each input, as part A's configuration says (decision D26). Eval-family
inputs are also run as eval requests under E0 to E7's flags in configuration V.

**The commits, named before they exist.**
- **The rule commit** carries this file.
- **The vectors commit** is the default-branch merge that adds the `syntax` area to `conformance/vectors/` at its
  corpus revision. It descends from the rule commit, and it is an ancestor of the first merge that adds a file under
  `Broiler.ECMAScript/Broiler.ECMAScript.Frontend/`.
- **The library commit** is the default-branch merge that completes SJL-13.2 (its done-when), named in bundle
  `sjl-13-001`. Every part is run there, from a clean checkout.

A part run at any other commit, or whose vectors commit fails those conditions, is void.

## Part A - the parse corpus and test262's syntax tests at library level, as classified

```rule
evidence class: conformance (not a measurement)
population: every input of conformance/parse/corpus.manifest as the rule commit holds it, under its goal and each of its variants, in configurations V and J; the eval-family inputs also as eval requests under E0 to E7 in configuration V; the test262 inputs read from test262 at ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e and each checked against the manifest's SHA-256, the others from conformance/parse/inputs/
configuration: the library's corpus runner (Broiler.ECMAScript.Frontend.Tests' corpus mode, or docs/evidence/sjl-13-001/tools/frontend-corpus) at the library commit, built in Release, on one thread of the stack the library documents for its tree-depth bound, no LANG and TZ=UTC; for each input, variant and configuration: the outcome (accepted, refused or crashed), and for a refusal the first diagnostic's shared code, phase and span, and for a refusal at either limit, nesting or tree depth, the depth the library reports; in configuration V and in configuration J the runner's regular-expression provider answers as that configuration's engine validator answered the input in its SJL-12 after baseline, as the classification records it (decision D26): it refuses a literal, naming the first row the classification names that the stage table bases on the provider, exactly when the classification expects the input refused in that configuration with such a row among the rows it names first, and admits every other literal; a json variant is read under the Script goal, as SJL-12's tool read it
completeness: every input, variant and configuration reported; the manifest's digest and the classification's equal to the rule commit's
controls: a copy of the library commit's report with one outcome flipped and one first code changed to a code no row allows, which the comparison must report as two unexplained inputs; retained
comparison: by a script committed to this bundle's tools/ after this file and before any run of this part is taken, which reads the runner's report in the fields this part's configuration lists, the classification and the catalogue's shared codes, and holds no expectation of its own; per input, variant and configuration: the outcome against the classification's expectation, and for a refusal the first code against the classification's rows
admitted outcomes, each by the classification and listed by reason in the bundle: an expected acceptance accepted; an expected refusal refused with a first code of the rows it names; an input the classification marks semantics-only accepted or refused; and L, an input the classification expects accepted that is refused at a limit, with the contract's nesting diagnostic (EX0011's shared code) at a reported depth past the nesting parameter the configuration sets, or with its tree-depth diagnostic (EX0012's shared code) at a reported depth past the library's tree-depth constant (decision D14)
every other outcome is a failure: an input crashed; an expected acceptance refused outside L; an expected refusal accepted unless semantics-only; a refusal whose first code no row of the classification allows
MET if and only if the run is complete, the control is reported, and every input in both configurations has an admitted outcome.
NOT MET otherwise.
```

**How it is read.** The classification was fixed before the library existed, so the library cannot shape its own
expectations. Its comparison script, which reads what the runner writes and so is committed once the runner's report
exists, reads its expectations from the classification and the catalogue alone, so it cannot move one either. A
failure is a library defect, fixed before the library commit, or a classification error, which only a new dated rule
file may correct, quoting this one. The per-reason lists and their tallies stay in the bundle.

## Part B - the syntax vectors at library level (template T3)

```rule
evidence class: conformance (not a measurement)
population: every vector of the syntax area at the vectors commit's corpus revision
configuration: the library-level runner, through its frontend adapter, at the library commit, Release: each vector's in parsed under the goal its op names (ParseScript, or eval code under PerformEval), with the library's default request; error: SyntaxError read as a refusal and any other outcome as an acceptance; a divergence vector compared with its out, and an impl-defined vector with its out unless it names the library
controls: a copy of one vector with its expected outcome changed to the opposite, which the runner must report; retained
MET if and only if every vector's outcome at library level is its expectation, and the control is reported.
NOT MET otherwise.
```

## Part C - determinism (layer L13 and rule SL9)

```rule
evidence class: determinism (not a measurement)
population: rule SL9's, as registered: the whole vector corpus, read and validated afresh each time, through the library-level runner, syntax vectors included; and every input and variant of part A's population in configuration V
configuration: rule SL9's test at the library commit; and the corpus runner's dump mode, which writes per input the SHA-256 of a canonical dump of the tree (each node's kind, span, raw facts and children in tree order), the side tables in their own order, and the ordered diagnostics; in three processes - run 1 in the manifest's order, run 2 in the reverse order, and run 3 on two threads, each input parsed twice concurrently, once on each
controls: two patches to scratch copies of the library commit, never merged, retained with their output: K1 a process-wide counter written into a node's raw fact - run 2 must differ from run 1, and rule SL2's witness must refuse it; K2 a side table filled from a HashSet<string>, whose order follows the process's string-hash seed - run 1 against run 2 must differ, and rule SL9 must fail; each reverted, and the revert checked
admitted difference classes: none
MET if and only if rule SL9 passes, every input gives one hash in all four parses, and both controls behave as stated and agree again after their revert.
NOT MET otherwise.
```

## Part D - the Native AOT probe (layer L10)

```rule
evidence class: publish and run (not a measurement)
population: Broiler.ECMAScript.Probe at the library commit, which references the frontend and runs the syntax vectors
configuration: the probe published with Native AOT and with JIT, with Broiler.VM's root settings (TrimMode full, warnings as errors), by .github/workflows/shared-libraries.yml at the library commit, and the same locally on linux-x64; each output retained
MET if and only if both publications succeed with no trim, AOT or single-file warning, and the two runs' answers are identical, line for line, and pass.
NOT MET otherwise.
```

## Part E - the shared libraries' rules (rule runs)

```rule
evidence class: rules (not a comparison)
population: rules SL1 to SL10 of Broiler.ECMAScript/Broiler.ECMAScript.Architecture.Tests/rules.register.json with their witnesses, SL1's layering row for Broiler.ECMAScript.Frontend (Algorithms and Broiler.Unicode.Algorithms), the library's PublicAPI.txt, package validation, and the assurance check over assurance.config.json's projects
configuration: dotnet test of the architecture tests in Release at the library commit; the API baseline and package validation by eng/pack.ps1 -ProjectSet Shared; the assurance check as the shared workflow runs it; logs retained
MET if and only if every rule passes with the frontend in scope, every witness is refused, the public API equals PublicAPI.txt in both directions, package validation passes, and the assurance check passes.
NOT MET otherwise.
```

## Part F - fuzzing at library level (layer L9)

```rule
evidence class: robustness (not a measurement)
population: the seeds docs/evidence/sjl-13-001/inputs/fuzz-seeds.txt names, each with its goal, its configuration and its count of mutants, and the time bound per parse that file states
configuration: the library's fuzz target at the library commit, Release, on the documented stack; each mutant parsed under its request
reading: a parse returns a tree or diagnostics; a finding is an exception escaping the library, a lost process, a parse past the time bound, or a diagnostic with no shared code of the catalogue
controls: a scratch copy in which the parser throws on one production the seeds reach; the target must report a finding; reverted, and the revert checked
MET if and only if no finding is reported and the control is reported as stated.
NOT MET otherwise.
```

## Part G - the limits (the contract's "limits checked before the stack is")

```rule
evidence class: robustness (not a measurement)
population: the deep-nesting corpus docs/evidence/sjl-13-001/inputs/deep/ as committed with this rule: for each of nesting through brackets, parentheses, blocks, functions, templates and their substitutions, and the four left-operand shapes of row R14 of the correspondence in Broiler.VM's bundle sjl-13-001 (a binary chain whose first operand is a parenthesised chain, a template whose substitution holds one, a binary chain whose first operand is a member or call chain, and a member or call chain whose object is a parenthesised binary chain), sources at the bound, one past it, and far past it, each with its expected outcome under the parameters it names
configuration: the corpus runner at the library commit, on the stack the library documents, each source parsed alone in its own process; and the library's derivation of the tree-depth constant, as JSD-0022 derived Broiler.VM's, retained with its measured frames per level and its margin
MET if and only if every source at or below its bound is accepted, every source past it is refused with its kind's diagnostic, EX0011's shared code for a nesting source and EX0012's for a tree-depth source, no process ends without its exit code, and the derivation admits the constant the library states on the stack it documents.
NOT MET otherwise.
```

## Part H - the contract's checklist (clause 1; record consistency)

```rule
evidence class: record consistency (not a comparison)
population: Broiler.ECMAScript.Frontend's PublicAPI.txt and its project file at the library commit; JSD-0080's output table, request, providers and limits as Broiler.VM's commit the bundle names holds them; the stage table
configuration: a script committed to this bundle's tools/ after this file and before any run of this part is taken, run at the library commit, that lists for each row of JSD-0080's output table and each request, provider and limit the contract names the public member that carries it, or "absent"; lists every public type of the library whose namespace, base type, field or parameter names a type of Broiler.VM or Broiler.JS (none is expected); and lists every engine row of the stage table for which the library has a diagnostic producer (none is expected)
controls: a scratch copy of the library commit with one contract member removed from PublicAPI.txt and the code, and one with a public stub type declared in a namespace of the script's engine-namespace list (Broiler.VM., Broiler.JavaScript.); the script must report both
MET if and only if every row and item the contract names has a member, except the caller's lexical names, which the script lists as absent with decision D16's reason, no public type names an engine type, no engine row has a producer, and both controls are reported.
NOT MET otherwise.
```

## Revisions

A rule revised after any run it judges has been seen is a new dated file in this directory. It quotes this one whole
and says it was written afterwards, and its decision is then weaker evidence.

## What this file does not decide

- **Broiler.VM's clauses** (4 to 10): its bundles `sjl-13-001` and `-002` decide them.
- **Broiler.JS's front end**, which SJL-14 changes, and this repository's bundle `sjl-14-001`.
- **The semantic model**, SJL-15's.
- **A speed claim of any kind.** The library is not measured here.
````
`````
``````
