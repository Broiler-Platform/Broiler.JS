# capture.ps1: one record of this lane's machine, as docs/roadmap/Measurement.md's section "Controlled lanes and
# effective configuration" asks: the CPU model and microcode, the core topology, the memory, the OS and kernel, the
# power plan and thermal policy, and the .NET SDK and runtimes. It reads only: it changes no setting, and kills nothing.
# Run with Windows PowerShell 5.1: powershell -NoProfile -File capture.ps1 > machine.txt
# The machine's name and its users are not recorded.
$ErrorActionPreference = 'Stop'

function Hex4([byte[]] $bytes) {
    # The registry keeps a revision as little-endian bytes.
    if ($null -eq $bytes) { return '(absent)' }
    $value = [uint64]0
    for ($i = $bytes.Length - 1; $i -ge 0; $i--) { $value = ($value -shl 8) -bor $bytes[$i] }
    return ('0x{0:X8}' -f $value)
}

# A power setting's AC value on the active scheme: the scheme's own override when it has one, its default otherwise.
# Read from the registry, because powercfg hides several of them (boost, cooling policy, core parking).
function PowerSetting([string] $scheme, [string] $sub, [string] $setting) {
    $override = "HKLM:\SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes\$scheme\$sub\$setting"
    $default = "HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\$sub\$setting\DefaultPowerSchemeValues\$scheme"
    $o = Get-ItemProperty $override -ErrorAction SilentlyContinue
    if ($null -ne $o -and $null -ne $o.ACSettingIndex) { return "$($o.ACSettingIndex) (the scheme's override)" }
    $d = Get-ItemProperty $default -ErrorAction SilentlyContinue
    if ($null -ne $d -and $null -ne $d.ACSettingIndex) { return "$($d.ACSettingIndex) (the scheme's default)" }
    return '(not set)'
}

"time: $((Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'))"

"== CPU"
$cpus = @(Get-CimInstance Win32_Processor)
$reg = Get-ItemProperty 'HKLM:\HARDWARE\DESCRIPTION\System\CentralProcessor\0'
foreach ($c in $cpus) {
    "model: $($c.Name.Trim())"
    "vendor: $($c.Manufacturer)"
    "identifier: $($reg.Identifier)"
    "processor id (CPUID signature and features): $($c.ProcessorId)"
    "microcode (Update Revision): $(Hex4 $reg.'Update Revision')"
    "microcode before this boot's update (Previous Update Revision): $(Hex4 $reg.'Previous Update Revision')"
    "base clock MHz: $($c.MaxClockSpeed)"
    "L2 KiB: $($c.L2CacheSize); L3 KiB: $($c.L3CacheSize)"
}

"== topology"
"sockets: $($cpus.Count)"
"physical cores: $(($cpus | Measure-Object NumberOfCores -Sum).Sum)"
"logical processors: $(($cpus | Measure-Object NumberOfLogicalProcessors -Sum).Sum)"
"SMT: $(if ((($cpus | Measure-Object NumberOfLogicalProcessors -Sum).Sum) -gt (($cpus | Measure-Object NumberOfCores -Sum).Sum)) { 'on' } else { 'off' })"
"[Environment]::ProcessorCount: $([Environment]::ProcessorCount)"

"== memory"
$cs = Get-CimInstance Win32_ComputerSystem
"total bytes: $($cs.TotalPhysicalMemory)"
foreach ($m in Get-CimInstance Win32_PhysicalMemory) {
    "module: $($m.Capacity) bytes, rated $($m.Speed) MT/s, configured $($m.ConfiguredClockSpeed) MT/s, part $($m.PartNumber.Trim()), bank $($m.BankLabel), slot $($m.DeviceLocator)"
}

"== board and firmware"
$board = Get-CimInstance Win32_BaseBoard
$bios = Get-CimInstance Win32_BIOS
"board: $($board.Manufacturer) $($board.Product)"
"firmware: $($bios.SMBIOSBIOSVersion), released $($bios.ReleaseDate.ToString('yyyy-MM-dd'))"
"system type: $(switch ($cs.PCSystemType) { 1 {'desktop'} 2 {'mobile'} default { "code $($cs.PCSystemType)" } })"

"== OS and kernel"
$os = Get-CimInstance Win32_OperatingSystem
$nt = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion'
"os: $($os.Caption.Trim()), $($nt.DisplayVersion), $($os.OSArchitecture)"
"kernel: $($os.Version).$($nt.UBR) (ntoskrnl.exe $((Get-Item "$env:SystemRoot\System32\ntoskrnl.exe").VersionInfo.ProductVersion))"
"hypervisor present: $($cs.HypervisorPresent)"
$dg = Get-CimInstance -ClassName Win32_DeviceGuard -Namespace root\Microsoft\Windows\DeviceGuard -ErrorAction SilentlyContinue
if ($dg) {
    "virtualization-based security: status $($dg.VirtualizationBasedSecurityStatus) (0 off, 1 configured, 2 running); security services running: $(@($dg.SecurityServicesRunning) -join ',') (1 Credential Guard, 2 memory integrity)"
}
try {
    $mp = Get-MpComputerStatus
    "Defender real-time protection: $($mp.RealTimeProtectionEnabled)"
} catch { "Defender real-time protection: (not readable)" }
"last boot: $($os.LastBootUpTime.ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'))"

"== power and thermal policy"
$battery = @(Get-CimInstance Win32_Battery -ErrorAction SilentlyContinue)
"power source: $(if ($battery.Count -eq 0) { 'mains (AC); no battery' } else { "battery present, status $($battery[0].BatteryStatus)" })"
$active = (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes').ActivePowerScheme
$names = @{ '381b4222-f694-41f0-9685-ff5bb260df2e' = 'Balanced'; '8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c' = 'High performance';
            'a1841308-3541-4fab-bc81-f71556f20b4a' = 'Power saver'; 'e9a42b02-d5df-448d-aa00-03f14749eb61' = 'Ultimate Performance' }
"active scheme: $active ($(if ($names.ContainsKey($active)) { $names[$active] } else { 'custom' }))"
$proc = '54533251-82be-4824-96c1-47b60b740d00'
"AC minimum processor state %: $(PowerSetting $active $proc '893dee8e-2bef-41e0-89c6-b55d0929964c')"
"AC maximum processor state %: $(PowerSetting $active $proc 'bc5038f7-23e0-4960-96da-33abaf5935ec')"
"AC processor boost mode: $(PowerSetting $active $proc 'be337238-0d82-4146-a960-4f3749d470c7') (0 disabled, 1 enabled, 2 aggressive, 3 efficient enabled, 4 efficient aggressive)"
"AC system cooling policy: $(PowerSetting $active $proc '94d3a615-a899-4ac5-ae2b-e4d8f634367f') (0 passive, 1 active)"
"AC core parking minimum cores %: $(PowerSetting $active $proc '0cc5b647-c1df-4637-891a-dec35c318583')"
"AC sleep after seconds: $(PowerSetting $active '238c9fa8-0aad-41ed-83f4-97be242c8f20' '29f6c1db-86da-48c5-9fdb-f2b67b1f44da') (0 never)"
"AC hibernate after seconds: $(PowerSetting $active '238c9fa8-0aad-41ed-83f4-97be242c8f20' '9d7815a6-7ee4-497e-8888-515a05f02364') (0 never)"

"== .NET"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'; $env:DOTNET_NOLOGO = '1'
"dotnet --version: $(dotnet --version)"
"RID: $((dotnet --info | Select-String -Pattern '^\s*RID:\s*(\S+)').Matches[0].Groups[1].Value)"
"SDKs:"
dotnet --list-sdks | ForEach-Object { "  $_" }
"runtimes:"
dotnet --list-runtimes | ForEach-Object { "  $_" }
