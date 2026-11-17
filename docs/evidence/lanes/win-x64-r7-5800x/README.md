# Lane `win-x64-r7-5800x`

**What this is.** One lane record, as [`Measurement.md`](../../../roadmap/Measurement.md)'s section "Controlled lanes
and effective configuration" asks: one machine and one RID, `win-x64`, recorded on 2026-11-16 before the first
measurement taken on it, part B of [bundle `sjl-3-001`'s rule](../../sjl-3-001/decision-rule.md). The record itself is
[`machine.txt`](machine.txt), written by [`capture.ps1`](capture.ps1), which reads the machine and changes nothing. The
machine's name and its users are not recorded. **It judges nothing**: a measurement on this lane is judged by its own
rule, in its own bundle.

**Where a lane record lives.** The repository had no convention for one; this folder proposes it. Each lane is a
folder `docs/evidence/lanes/<lane>/`, named for the RID and the machine, holding this README (the reading), the capture
script and its output. A sub-bundle measured on the lane names the folder and the SHA-256 of its `machine.txt` in its
own manifest, written before its first run, and records its own run window (the load and the processes beside it) with
its runs. A change to the machine (its firmware, microcode, memory, OS build, SDK or runtime) is a new dated capture
beside the first, never an edit of it. One lane serves every measurement that names it; Phase-0's item 0-8 lists the
others waiting for a lane.

## The machine

| Field | Value |
|---|---|
| CPU | AMD Ryzen 7 5800X 8-Core Processor (family 25, model 33, stepping 0: Zen 3, one CCD), base clock 3.8 GHz |
| Microcode | `0x0A20102B` (the same before and after this boot's update) |
| Topology | one socket, 8 physical cores, 16 logical processors (SMT on); L2 4 MiB, L3 32 MiB shared |
| Memory | 32 GiB: two 16 GiB DDR4 modules, 2666 MT/s rated and configured, one per channel (A and B) |
| Board and firmware | Gigabyte A320M-S2H-CF, firmware F58d (2024-09-02); a desktop |
| OS and kernel | Windows 11 Enterprise 25H2, 64-bit, build 10.0.26200.9974 |
| Hypervisor | present; virtualization-based security running (Credential Guard), so Windows runs as the hypervisor's root partition |
| Defender | real-time protection on |
| Power | mains (AC), no battery; the High performance plan, active before this record and not changed by it: processor state 100 % minimum and maximum, core parking off (100 % minimum cores), no sleep or hibernation |
| Boost and thermal policy | processor boost mode 2, aggressive (the plan's default): AMD Precision Boost is on; system cooling policy active (fan before throttling) |
| .NET | SDK 10.0.404 (the arms' builds and runs; 10.0.100-rc.1 is also installed and unused), runtime Microsoft.NETCore.App 10.0.15 (8.0.34 also installed); RID `win-x64` |

## Readings

- **The RID.** Phase-0's item 0-8 and [`SharedLibraries.md`](../../../roadmap/SharedLibraries.md) ("Clause 7's
  measurement half: the owner's decision of 2026-10-20") ask for linux-x64 first. This machine is `win-x64`. Part B of
  `sjl-3-001`'s rule names "one machine and one RID, both named in the sub-bundle's manifest" and fixes neither, so the
  reading is that a `win-x64` lane can judge part B; and what part B does not decide includes "Anything about a RID or a
  machine other than the one named", so a reading here says nothing about linux-x64. **The alternative** is a
  linux-x64 lane: a dedicated Linux host, which does not exist, or this hardware booted into a native Linux (WSL 2 is
  not that: it is a guest of the same hypervisor). **The owner confirmed this lane and its RID on 2026-11-17**, before
  any run on it, by answering the session's question "Should this workstation be part B's controlled lane, quieted for
  the run window?" with "Confirm win-x64 lane".
- **Dedication.** `SharedLibraries.md` reads "a dedicated host the owner provisions". This machine is the owner's
  workstation, declared as the lane for a run window and quieted for it as the sub-bundle measured in that window
  records; it is not dedicated outside one.
- **What is not controlled,** and is recorded rather than changed: Precision Boost (on, as the plan has it; the
  firmware's PBO setting cannot be read from the OS), SMT, the hypervisor and virtualization-based security, Defender's
  real-time scanning, Windows' own services (Update, the search indexer), the room's temperature, and the Claude
  desktop app that starts the run and waits idle during it.
- **What came before on `win-x64`.** Phase-0's item 0-8 records a historical `win-x64` A/A attempt on a developer
  workstation, under normal load and not quieted, that failed its provisional 7.5 % band. That band is not part B's
  tolerance: part B holds each workload to the A/A floor N of its own run.
