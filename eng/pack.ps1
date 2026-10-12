# Pack one version stream's shipping projects (decision SJL-D-3: two streams in one repository).
#   -ProjectSet Engine  every project of the solution outside Broiler.ECMAScript/: the engine's packages,
#                       at one X.Y.Z-preview.N version, tagged v*.
#   -ProjectSet Shared  the projects of Broiler.ECMAScript.slnf: the shared libraries' packages, at their own
#                       version, tagged ecmascript-v*. X.Y.Z-probe.N is accepted here too, for a probe
#                       pre-release that is packed for a committed feed and never published (milestone SJL-2.13).
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')][string] $Configuration = 'Release',
    [ValidateSet('Engine', 'Shared')][string] $ProjectSet = 'Engine',
    [string] $Version,
    [string] $Output = 'artifacts'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    $solution = @(Get-ChildItem -File -Filter *.slnx)
    if ($solution.Count -ne 1) { throw 'Expected exactly one solution.' }
    [xml] $xml = Get-Content -Raw $solution[0].FullName
    $sharedFolder = 'Broiler.ECMAScript/'
    if ($ProjectSet -eq 'Shared') {
        $filter = Get-Content -Raw 'Broiler.ECMAScript.slnf' | ConvertFrom-Json
        $paths = @($filter.solution.projects)
        foreach ($path in $paths) {
            if (!$path.StartsWith($sharedFolder)) { throw "The shared filter names $path, outside $sharedFolder." }
        }
    } else {
        $paths = @($xml.SelectNodes('//Project[@Path]') | ForEach-Object { $_.Path } | Where-Object { !$_.Replace('\', '/').StartsWith($sharedFolder) })
    }
    $properties = @("-p:Configuration=$Configuration")
    if ($Version) {
        $pattern = if ($ProjectSet -eq 'Shared') { '^\d+\.\d+\.\d+-(preview|probe)\.[1-9]\d*$' } else { '^\d+\.\d+\.\d+-preview\.[1-9]\d*$' }
        if ($Version -notmatch $pattern) { throw "Expected X.Y.Z-preview.N$(if ($ProjectSet -eq 'Shared') { ' or X.Y.Z-probe.N' })." }
        $properties += "-p:Version=$Version", "-p:PackageVersion=$Version"
    }
    $packages = @(
        foreach ($path in $paths) {
            $result = & dotnet msbuild $path -nologo @properties '-getProperty:IsPackable,PackageId,PackageVersion,IncludeBuildOutput,IncludeSymbols'
            if ($LASTEXITCODE -ne 0) { throw "Cannot evaluate $path." }
            $metadata = ($result -join "`n" | ConvertFrom-Json).Properties
            if ($metadata.IsPackable -eq 'true') {
                [pscustomobject]@{ Project = $path; Metadata = $metadata }
            }
        }
    )
    if (!$packages.Count) { throw 'No packable projects found.' }
    if (@($packages.Metadata.PackageVersion | Select-Object -Unique).Count -ne 1) { throw 'Package versions must agree.' }
    if (@($packages.Metadata.PackageId | Select-Object -Unique).Count -ne $packages.Count) { throw 'Duplicate package IDs.' }
    $outputPath = [IO.Path]::GetFullPath([IO.Path]::Combine((Get-Location).Path, $Output))
    # Refuse stale packages rather than silently uploading an earlier version.
    if (Test-Path $outputPath) {
        if (Get-ChildItem -LiteralPath $outputPath -File | Where-Object Extension -in '.nupkg', '.snupkg') {
            throw "Package output must be empty: $outputPath"
        }
    }
    foreach ($package in $packages) {
        & dotnet pack $package.Project -c $Configuration --nologo @properties -o $outputPath
        if ($LASTEXITCODE -ne 0) { throw "Packing $($package.Project) failed." }
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $expected = @{}
    foreach ($package in $packages) { $expected[$package.Metadata.PackageId] = $package.Metadata.PackageVersion }
    $archives = @(Get-ChildItem -LiteralPath $outputPath -Filter *.nupkg)
    if ($archives.Count -ne $packages.Count) { throw "Expected $($packages.Count) packages; found $($archives.Count)." }
    foreach ($package in $packages) {
        $metadata = $package.Metadata
        $path = Join-Path $outputPath "$($metadata.PackageId).$($metadata.PackageVersion).nupkg"
        $zip = [IO.Compression.ZipFile]::OpenRead($path)
        try {
            $entry = @($zip.Entries | Where-Object FullName -like '*.nuspec')
            if ($entry.Count -ne 1) { throw "Expected one nuspec in $path." }
            $reader = [IO.StreamReader]::new($entry[0].Open())
            try { [xml] $nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
            $manifest = $nuspec.package.metadata
            if ($manifest.id -ne $metadata.PackageId -or $manifest.version -ne $metadata.PackageVersion) { throw "Incorrect identity in $path." }
            # An unset <Description> is not an error to the SDK: it silently packs the
            # literal 'Package Description', which is what the nuget.org listing then shows.
            $description = "$($manifest.description)".Trim()
            if (!$description -or $description -eq 'Package Description') { throw "Set a <Description> for $($metadata.PackageId)." }
            foreach ($asset in @('README.md', 'icon.png')) {
                if (!$zip.GetEntry($asset)) { throw "Missing $asset in $path." }
            }
            foreach ($dependency in $nuspec.SelectNodes('//*[local-name()="dependency"]')) {
                if ($expected.ContainsKey($dependency.id) -and $dependency.version -ne $expected[$dependency.id]) {
                    throw "Incorrect internal dependency $($dependency.id) $($dependency.version) in $path."
                }
            }
            if ($metadata.IncludeBuildOutput -ne 'false') {
                if (!@($zip.Entries | Where-Object FullName -like 'lib/*.dll').Count) { throw "Missing assembly in $path." }
                if (!@($zip.Entries | Where-Object FullName -like 'lib/*.xml').Count) { throw "Missing API documentation in $path." }
                if ($metadata.IncludeSymbols -eq 'true' -and !(Test-Path ($path -replace '\.nupkg$', '.snupkg'))) { throw "Missing symbols for $path." }
            }
        } finally { $zip.Dispose() }
    }
    Write-Host "Verified $($packages.Count) $ProjectSet packages at $($packages[0].Metadata.PackageVersion)."
} finally { Pop-Location }
