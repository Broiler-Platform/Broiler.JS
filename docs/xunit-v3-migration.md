# xUnit v3 migration and Native AOT compiler selection

Date: 2026-10-29.

## Test framework

All 19 test projects in the engine and shared ECMAScript libraries use
`xunit.v3` 4.0.2, with `xunit.runner.visualstudio` 4.0.1 and
`Microsoft.NET.Test.Sdk` 18.11.0. Test assemblies are executables, as required
by xUnit v3. The vector worker and reference-arm comparison retain their custom
entry points, and delegate ordinary test invocations to the xUnit executable
runner.

The repository keeps its existing VSTest-based `dotnet test`, filters, TRX and
coverage contract. `IsTestingPlatformApplication=false` is set for all test
projects, including the shared libraries whose props are intentionally separate.
This prevents the xUnit 4.0 package's default MTP v2 integration from rejecting
VSTest commands under .NET 10. It does not change the test framework back to v2.
No SDK version lock or global runner setting was added.

Most engine tests execute synchronous compiled JavaScript and cannot
cooperatively cancel a running execution. The former 4,683 per-test `Timeout`
attributes have been removed. VSTest's process hang collector now terminates a
test host when no test completes for ten minutes. The separately scheduled,
single-test engine corpus has a ninety-minute limit matching its maximum
existing CI job budget. CLI MSBuild properties can override these defaults.
The watchdog applies to VSTest invocations; standalone console/reference-worker
entry points continue to use their own command/worker budgets.

Asynchronous test waits now use `await` and the xUnit cancellation token. Other
fixes include typed theory-row access, nullable theory parameters, removal of an
unused theory parameter, unambiguous assertions and the v3 assembly name in the
architecture witness. Shared source files retain the required LF line endings.

Official references:

- [Migrating from xUnit v2 to v3](https://xunit.net/docs/getting-started/v3/migration)
- [xUnit v3 runner/platform choices](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
- [MSBuild test-platform properties](https://learn.microsoft.com/dotnet/core/tools/csproj#istestingplatformapplication)

## Warnings and test failures

No new warning suppressions were introduced. The existing root legacy
suppression policy was retained; a zero-warning build does not mean that
pre-existing suppressed nullable/platform/documentation debt has all been removed.

Visible compiler/analyzer warnings were addressed through explicit member hiding,
nullable guards, qualified documentation overloads and appropriate reflection
contracts. The expression model retains trim/AOT analysis. Reflection helpers
that construct arbitrary generic/array types now declare their requirements.
Array cloning and LINQ conversion reuse an existing array `Type`, avoiding an
unnecessary dynamic type construction on those paths.

The legacy host's NuGet dependencies pulled in vulnerable
`System.Security.Cryptography.Pkcs` 8.0.1. A direct, centrally versioned 10.0.14
reference overrides that transitive dependency; auditing remains enabled.
The obsolete `AssemblyName.HashAlgorithm` call was also removed from the
single-module image writer.

The test run exposed and fixed these additional issues:

- Windows ignores the Unix `TZ` variable. The test fixtures now set/reset the
  process-local timezone cache consistently, using the same mechanism as the
  existing named-zone fixtures. The OS timezone and production timezone behavior
  are unchanged.
- A property-cache diagnostic assumed globally interned shapes were cold.
  Its input now uses a fresh shape prefix so prior tests cannot invalidate its
  transition-count assertion.
- Blocking-host concurrency tests now use the existing nonparallel execution
  collection, keeping their process-wide diagnostics and thread-pool timing
  independent of other test classes.
- `Promise.race` had an unused synchronization-context lookup that rejected a
  valid host with no synchronization context. It now uses the same promise/job
  machinery without that obsolete prerequisite.
- `JSUndefined.IsUndefined` relied on a factory cache populated by the Engine's
  module initializer. It now recognizes the Runtime-owned undefined type without
  requiring another assembly to initialize first.

## Why ILCompiler appears version-locked

There is no `Microsoft.DotNet.ILCompiler` version declaration, blacklist or
blocked-version rule in Broiler.JS. It is an **implicit .NET SDK reference**
introduced by `PublishAot=true` in the Native AOT projects.

The evaluated toolchain on this machine was:

| Item | Selected value |
| --- | --- |
| .NET SDK | `10.0.403` |
| `KnownILCompilerPack`, target framework | `net10.0` |
| SDK `ILCompilerPackVersion` | `10.0.14` |
| Implicit `Microsoft.DotNet.ILCompiler` reference | `10.0.14`, `PrivateAssets=all` |
| Windows compiler download | `runtime.win-x64.Microsoft.DotNet.ILCompiler`, exact range `[10.0.14]` |

The version comes from the installed SDK's
`Microsoft.NETCoreSdk.BundledVersions.props`; the implicit reference and exact
download are added by `Microsoft.NET.Sdk.FrameworkReferenceResolution.targets`.
That SDK ownership explains why a package-management UI can show it as locked
or not independently upgradable. It is not a missing NuGet source or a
repository restore lock.

NuGet.org's version index contained 10.0.14, and restoring the Native AOT sample
for `win-x64` succeeded. No SDK file or compiler-package override was changed.
For a newer Native AOT compiler, use an SDK/toolchain cohort that selects it;
do not add an unrelated central version override merely to make the UI editable.

To inspect the selection without publishing:

```powershell
dotnet msbuild Broiler.JS/samples/Broiler.JavaScript.NativeAotSample/Broiler.JavaScript.NativeAotSample.csproj -nologo -target:ProcessFrameworkReferences -getProperty:NETCoreSdkVersion,RuntimeFrameworkVersion,IlcPackageVersion -getItem:KnownILCompilerPack,PackageReference,PackageDownload
dotnet restore Broiler.JS/samples/Broiler.JavaScript.NativeAotSample/Broiler.JavaScript.NativeAotSample.csproj -r win-x64
```

`IlcPackageVersion` itself can be empty: inspect `KnownILCompilerPack` and the
generated reference/download items rather than assuming an empty property means
the SDK has not selected a compiler.

## Verification

Results on the local Windows .NET 10 toolchain:

| Check | Result |
| --- | --- |
| All 19 regular test assemblies | 12,893 passed; zero failures or skips |
| Debug build, warnings as errors | Zero warnings/errors |
| Clean Release rebuild, warnings as errors | Zero warnings/errors |
| NuGet audit, including transitive dependencies | No known vulnerabilities |
| Native AOT sample `win-x64` restore | Passed; SDK-selected compiler 10.0.14 |

The final test summary uses the latest run of each assembly, including the full
5,370-test Integration rerun after its fixture corrections. Earlier diagnostic
failure logs remain under the evidence directory. Test results do not include
the separately scheduled engine-corpus acceptance category.

Build and test from the repository root:

```powershell
dotnet build Broiler.JS.slnx -c Release -m:1 -warnaserror
dotnet test Broiler.JS.slnx -c Release -m:2 --filter "Category!=EngineCorpus"
dotnet list Broiler.JS.slnx package --vulnerable --include-transitive
```

The large engine corpus remains a separate acceptance job in CI; the migration's
regular unit-test command excludes that category. Local build, test, audit and
SDK-selection evidence is saved under `artifacts/xunit-v3/`.
