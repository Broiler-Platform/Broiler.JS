# Publishing preview packages

Broiler.JS publishes to **nuget.org and nowhere else**. There is no GitHub Packages feed,
no internal feed, and no per-feed switch anywhere in the build: `NuGet.config`,
[`eng/verify-feed.ps1`](../eng/verify-feed.ps1),
[`eng/resolve-preview-version.mjs`](../eng/resolve-preview-version.mjs) and the workflows all
name one source.

## Two version streams

This repository publishes two sets of packages, each with its own version, tags and workflow
(decision SJL-D-3 of the shared JavaScript libraries programme):

| Stream | Projects | Version | Tags | Workflow |
|---|---|---|---|---|
| **Engine** | every project of `Broiler.JS.slnx` outside `Broiler.ECMAScript/` | `X.Y.Z-preview.N`, lockstep, floor in `eng/Broiler.Packaging.props` | `v*` | [`Publish`](../.github/workflows/publish.yml) |
| **Shared** | the projects of `Broiler.ECMAScript.slnf`, the `Broiler.ECMAScript.*` packages | `X.Y.Z-preview.N`, floor in `Broiler.ECMAScript/Directory.Build.props` | `ecmascript-v*` | [`Publish shared libraries`](../.github/workflows/publish-shared.yml) |

Each stream is packed by `eng/pack.ps1 -ProjectSet Engine` or `-ProjectSet Shared` and resolved by
`eng/resolve-preview-version.mjs` with `PROJECT_SET=engine` or `PROJECT_SET=shared`, which reads
nuget.org and that stream's tags only. A shared release does not move the engine's version, and an
engine release does not move the shared packages' - the alternative, lockstep, would bump
Broiler.VM's dependencies on every engine release whether or not a shared library changed.

Everything below describes the engine stream; the shared stream works the same way, with two
differences. *(Revised 2026-10-12, when the owner withdrew the prefix reservation and decision
SJL-D-2 was revised: the sentence said "three differences", and the first read "**The
`Broiler.ECMAScript` prefix must be reserved on nuget.org before anything under it is published**
(decision SJL-D-2; the programme's governance, section 8), which also guards against dependency
confusion. Reserving it is an application a release owner makes to nuget.org, and no workflow can
make it. **It is owed: it has not been made on this date.** When it is granted, the release owner
sets the repository variable `ECMASCRIPT_PREFIX_RESERVED` to `true`; until then `Publish shared
libraries` refuses to run." No reservation is required, and the workflow no longer reads that
variable.)*

- **Its dry run is [`Shared libraries`](../.github/workflows/shared-libraries.yml)**, on every push
  and pull request: it builds the folder and runs its rules, publishes and runs the Native AOT probe,
  runs the assurance check, packs the stream with package validation, restores it in an isolated
  cache as `eng/verify-feed.ps1` does, and runs a pristine consumer
  (`scripts/packaging/test_pristine_consumer.py --set shared`) from a local feed alone.
- **A probe pre-release is packed, never published.** `eng/pack.ps1 -ProjectSet Shared` also accepts
  `X.Y.Z-probe.N`, which the resolver never produces, so `Publish shared libraries` cannot push one.
  `0.0.1-probe.1` of `Broiler.ECMAScript.Algorithms`, the contract types alone, was packed at
  milestone SJL-2.13 for Broiler.VM's admission probe and reaches Broiler.VM through that
  repository's committed feed; its version is below every version the stream will publish, so it
  can never shadow one.

The shared packages' compatibility promise (the API baseline, package validation, the changelog
headings) is in [`public-api.md`](public-api.md).

*Added 2026-10-12, at milestone SJL-2.13. Until then this document described one stream: "Every
package in the solution ships at one version, in lockstep", which remains true of the engine
stream.*

## Running a publish

The [`Publish`](../.github/workflows/publish.yml) workflow is the only supported route.

1. **Rehearse in CI.** There is no dry-run mode in Publish. CI already builds, tests, packs,
   verifies every package and proves a consumer can restore the set from nuget.org on every
   push to `main` and every pull request that changes more than records, attaching the
   packages as an artifact — without pushing anything. A pull request confined to
   `docs/evidence/` and `docs/roadmap/SharedLibraries*.md`, which no test reads, skips CI's
   build and test jobs; `Shared libraries` still runs on it, and a push to `main` and Publish
   run CI in full whatever changed. CI's `CI result` job reports on every run, under that one
   name, whether the build and test jobs passed or were rightly skipped: it is the CI check to
   wait for, and the one a branch rule should require, because a skipped matrix job does not
   report its per-OS names. *(Revised 2026-10-19, when CI began to skip records-only pull
   requests: "on every push and pull request" became "on every push to `main` and every pull
   request that changes more than records".)*
2. **Dispatch Publish** from the Actions tab. Leave `version-suffix` empty so the version is
   resolved automatically. The run validates again against the version it resolves, then
   pushes. It also runs [`Shared libraries`](../.github/workflows/shared-libraries.yml) at the
   same commit, because CI leaves the `Broiler.ECMAScript` test projects to that workflow, which
   otherwise runs only on pushes and pull requests. *(Added 2026-10-19, when CI stopped running
   those projects itself.)*

Pushing a `v0.1.0-preview.N` tag publishes as well. Use it only to publish a specific number;
the automatic path is otherwise identical.

`NUGET_API_KEY` must exist as a repository secret. A run fails early and loudly if it does
not, rather than after a full build.

## How the version is chosen

`VersionPrefix`/`VersionSuffix` in [`eng/Broiler.Packaging.props`](../eng/Broiler.Packaging.props)
set the **floor** — the lowest version a publish may use. Every package of the engine stream ships
at one version, in lockstep; `eng/pack.ps1` refuses a set whose versions disagree. *(Revised
2026-10-12, at milestone SJL-2.13: "Every package in the solution ships at one version" became "Every
package of the engine stream", when the shared stream was added.)*

The number actually published is one past the highest preview number this repository has ever
**spent**, which is the union of two records:

- every version of every one of our package IDs that nuget.org reports, via the flat container,
  which unlike search results still lists unlisted versions; and
- every `v*` tag on this repository, which a successful publish run creates.

A number is spent when a run claims it, and it stays spent. The feed alone is not enough to
know that: a version can be deleted from nuget.org, and a run that pushed four packages and
then failed leaves the other sixteen absent from the feed entirely. Reading the feed alone
would hand that number to a second, different build. So if nuget.org reports `preview.2` while
a tag records `preview.3`, the next publish is `preview.4` — never a second `preview.3`.

That is why the version job checks out with full history and tags. **An unfetched tag reads as
an unspent number**, so a shallow checkout there would quietly reintroduce the bug.

`version-suffix` (or the tag name) overrides the choice, but only upwards: a request below the
computed next version is rejected.

## What every package carries

[`eng/Broiler.Packaging.props`](../eng/Broiler.Packaging.props) is vendored from the canonical
copy in the aggregate repository and supplies the shared metadata — Apache-2.0 license
expression, icon, README, third-party notices, XML documentation, `snupkg` symbols,
deterministic builds and Source Link. [`Directory.Build.props`](../Directory.Build.props) adds
this repository's project URL and tags, and marks the non-shipping projects.

Each shipping project sets its own `<Description>`. `eng/pack.ps1` fails the build if one is
missing: an unset description is not an error to the SDK, which silently packs the literal
`Package Description` and puts that on the nuget.org listing.

`eng/pack.ps1` also checks, for every package, that the nuspec identity matches, that the
README and icon are present, that internal dependencies point at the version being published,
and that a package with build output has an assembly, XML documentation and a symbol package.

## Dependencies outside this repository

`Broiler.JavaScript.BuiltIns` references `Broiler.Regex` and `Broiler.Unicode.Cldr.LocaleData`,
which are published from their own repositories. `eng/verify-feed.ps1` catches an unpublished
dependency before the push by restoring the whole set from a scratch project whose only sources
are the packages about to be published plus nuget.org.
