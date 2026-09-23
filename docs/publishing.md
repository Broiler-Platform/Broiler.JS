# Publishing preview packages

Broiler.JS publishes to **nuget.org and nowhere else**. There is no GitHub Packages feed,
no internal feed, and no per-feed switch anywhere in the build: `NuGet.config`,
[`eng/verify-feed.ps1`](../eng/verify-feed.ps1),
[`eng/resolve-preview-version.mjs`](../eng/resolve-preview-version.mjs) and the workflows all
name one source.

## Running a publish

The [`Publish`](../.github/workflows/publish.yml) workflow is the only supported route.

1. **Dispatch it** from the Actions tab. Leave `version-suffix` empty so the version is
   resolved automatically, and leave `dry-run` on for a rehearsal.
2. A **dry run** resolves the version, builds, tests, packs, verifies every package, proves a
   consumer can restore the set from nuget.org, and attaches the packages as an artifact —
   without pushing anything.
3. Re-dispatch with `dry-run` off to push. The same validation runs again against the version
   that run resolves.

Pushing a `v0.1.0-preview.N` tag publishes as well, and skips the dry-run default. Use it only
to publish a specific number; the automatic path is otherwise identical.

`NUGET_API_KEY` must exist as a repository secret. A non-dry run fails early and loudly if it
does not, rather than after a full build.

## How the version is chosen

`VersionPrefix`/`VersionSuffix` in [`eng/Broiler.Packaging.props`](../eng/Broiler.Packaging.props)
set the **floor** — the lowest version a publish may use. Every package in the solution ships
at one version, in lockstep; `eng/pack.ps1` refuses a set whose versions disagree.

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
