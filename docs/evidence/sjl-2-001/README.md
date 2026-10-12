# Bundle sjl-2-001: the shared libraries' home foundation

**Milestone:** SJL-2 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-2-the-admission-path.md`: its Broiler.JS
clauses 7 to 10, work packages SJL-2.10 to SJL-2.14. **Subject:** commit `a84a0b4` of this repository,
which the logs here were collected against. **Collected:** 2026-10-12. **Owner:** the Broiler.JS
maintainer. **Reviewer:** none; every role is one person, and nothing here was read by a second one.

SJL-2 has no predeclared decision rule (SJL-0.9 filed rules for the milestones that compare engines);
each clause below is read against the milestone file's exit gate as written.

## Clauses

| Clause | Reading | What the bundle holds |
|---|---|---|
| **7.** The shared folder builds under the conventions. SL1 to SL10 are `Active`, with witnesses. | **Met** | [`tests.log`](tests.log): the shared filter's tests, every one passed, among them each rule's clean direction, its witness direction and the register's own checks (`The_register_holds_SL1_to_SL10_each_Active`). [`injections/`](injections/): each rule watched failing on a defect injected into a real shared library and passing after the revert, one log per rule. |
| **8.** The API baseline and the assurance gate run in CI, and each is watched failing on an injected drift. | **Met locally; the CI half is owed** | The drift controls: [`injections/SL6.log`](injections/SL6.log) (a public member added without its baseline) and [`injections/assurance-drift.log`](injections/assurance-drift.log) (the assurance tool's `check`: clean, an unannotated unit, a body changed under its fingerprint, reverted). Both checks are in the [`Shared libraries`](../../../.github/workflows/shared-libraries.yml) workflow: SL6 in its test job, the assurance check in its own job. **No run of that workflow is retained here**: it runs on a pull request or on `main`, and none had happened when this bundle was collected. The first run's result is recorded against this clause when it exists. |
| **9.** The probe pre-release packs and restores in a pristine consumer, and passes package validation's first-version run. | **Met** | [`pack-probe.log`](pack-probe.log): `Broiler.ECMAScript.Algorithms` `0.0.1-probe.1` packed from `a84a0b4`, and package validation's run, "APICompat ran successfully without finding any breaking changes", with no baseline because none is published. [`pristine-consumer-shared.log`](pristine-consumer-shared.log): the package restored from a local feed alone (the `Broiler.ECMAScript` prefix mapped to it) into an isolated package cache, and a consumer of its contract types run. [`probe-package.txt`](probe-package.txt): its identity, content hash, nuspec and contents. |
| **10.** The Native AOT probe publishes with zero warnings and runs. | **Met on linux-x64** | [`probe-publish-linux-x64.log`](probe-publish-linux-x64.log): published with Broiler.VM's root settings, no warning. [`probe-run-linux-x64.log`](probe-run-linux-x64.log): the publish directory holds no managed assembly; the image runs every vector of corpus revision 3 with no failure; its report is byte-identical to the same probe's under the JIT. [`probe-report-aot.tsv.gz`](probe-report-aot.tsv.gz) is that report. The win-x64 publish and run are the workflow's; none is retained here. |

## The rules and their injections

| Rule | Injected into a real shared library | Log |
|---|---|---|
| SL1 | `Broiler.ECMAScript.Algorithms` references an engine project | [`SL1.log`](injections/SL1.log) |
| SL2 | A mutable static field in the algorithms library | [`SL2.log`](injections/SL2.log) |
| SL3 | File I/O in the algorithms library | [`SL3.log`](injections/SL3.log) |
| SL4 | Culture-sensitive casing in the algorithms library | [`SL4.log`](injections/SL4.log) |
| SL5 | The algorithms library suppresses a trim warning | [`SL5.log`](injections/SL5.log) |
| SL6 | A public member added without its baseline | [`SL6.log`](injections/SL6.log) |
| SL7 | One generated vector file edited by hand | [`SL7.log`](injections/SL7.log) |
| SL8 | The assurance configuration stops covering the Conformance library | [`SL8.log`](injections/SL8.log) |
| SL9 | The report gains a value that differs from run to run | [`SL9.log`](injections/SL9.log) |
| SL10 | A `#region` directive in the algorithms library | [`SL10.log`](injections/SL10.log) |

Each log ends with the two exit codes: the injected run's, non-zero, and the reverted run's, zero.
Every rule also has a witness file under
`Broiler.ECMAScript/Broiler.ECMAScript.Architecture.Tests/witnesses/` (SL9's is a type compiled into
the test assembly), which the rule's witness test refuses on every run; the register,
`rules.register.json`, names them.

## What a reader should know

- **SL2's first injection did not fail the rule, and the log is the second.** The injected field was
  never assigned, so warnings-as-errors refused the build, the previous build's assembly was read,
  and the rule passed. The injection was changed to assign the field, and the rerun failed as it
  should. Every injection into code (SL2's rerun, SL3, SL4, SL6, SL9 and SL10) shows its build
  succeeding before the rule ran; SL1, SL5, SL7 and SL8 read project files, the corpus or the
  configuration, and their injections needed no build.
- **SL4 has one stated limit.** A parameterless `ToString()` on a number is not refused, because
  every record's generated `ToString` reaches one. The register row says so; it is a review
  obligation of convention 6.
- **SL8 is split in two.** The C# rule holds the configuration, the artefacts and the workflow to the
  libraries; the unit-level half is the assurance tool's own `check`, which needs Broiler.Code beside
  this repository and so runs in CI rather than in the test project.
- **The assurance record is all `PENDING`.** [`assurance-status.txt`](assurance-status.txt): every
  relevant unit annotated, none human-reviewed. No record here claims a review.
- **The prefix reservation is owed.** Reserving `Broiler.ECMAScript` on nuget.org is an application a
  release owner makes, and it has not been made. `Publish shared libraries` refuses to run until the
  repository variable `ECMASCRIPT_PREFIX_RESERVED` records it ([`docs/publishing.md`](../../publishing.md)).
  Nothing has been published, and the probe pre-release never will be.
- **The resolver's tests** are [`resolve-preview-version-tests.log`](resolve-preview-version-tests.log),
  the three new ones among them: each stream reads only its own tags, a tag-requested version must
  carry its stream's prefix, and the streams partition the projects.
- **SJL-2.15 was not needed.** Broiler.JSeal is unchanged: decision SJL-D-2 took the neutral prefix
  `Broiler.ECMAScript`, outside both families its package gate knows.

## Not in this bundle

Broiler.VM's clauses 1 to 6, and the admission probe that restores this package there, are Broiler.VM's
bundle `docs/evidence/sjl-2-001/` in that repository. Neither bundle cites the other's evidence.
