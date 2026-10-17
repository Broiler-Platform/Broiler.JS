<!-- SPDX-FileCopyrightText: 2026 Broiler Platform contributors -->
<!-- SPDX-License-Identifier: Apache-2.0 -->

# IANA Time Zone Database pin

**Owner:** the Broiler.JS maintainer, as the data home's owner (decision SJL-D-1 (b-iii)). **Reviewer:** none.

The IANA Time Zone Database release both JavaScript engines' time zone data is generated from, archived
here unmodified. [`tzdb.pin`](tzdb.pin) records the tarball's length, SHA-256 and SHA-512, and the length
and SHA-256 of each member a generator reads. Broiler.VM's decision JSD-0076 (milestone SJL-8.1 of the
shared JavaScript libraries programme) makes this repository the data home and pins this release for both
engines, under decision SJL-D-20.

## Where it came from

**This directory is a copy of Broiler.VM's `src/tests/tzdb/pins/`**, taken at Broiler.VM `9508bd5` at work
package SJL-8.2. `tzdata2026e.tar.gz` and `tzdb.pin` are byte-identical to Broiler.VM's, with identical
hashes: nothing was retrieved again, and the pin's own comment, which tells how Broiler.VM took the
release, is kept unchanged. Only this README is rewritten. Broiler.VM's pin README records the retrieval:
twice on 2026-10-05 from `https://data.iana.org/time-zones/releases/`, into two files that `cmp` found
byte-identical, the first archived; IANA's detached PGP signature was not checked, because no key either
repository trusts signs it.

| | |
|---|---|
| Release | **tzdb 2026e**, released 2026-09-29; on 2026-10-17 still IANA's newest (JSD-0076) |
| Archive | [`tzdata2026e.tar.gz`](tzdata2026e.tar.gz), the release's data tarball as IANA publishes it at `https://data.iana.org/time-zones/releases/` |
| Members read | the nine source files `zic` compiles in this pin's default build - `africa`, `antarctica`, `asia`, `australasia`, `europe`, `northamerica`, `southamerica`, `etcetera`, `backward` - and `zone.tab`, `version` and `LICENSE`. `backzone` is not read (SJL-D-20): ECMA-402's primary identifiers are taken from CLDR, as its note recommends, and the offsets are the default build's. IANA's own `Makefile` also compiles `factory`; the policy leaves `Factory` out, so it is not read either |
| Licence | the release's `LICENSE`: the data is in the public domain |

## How the pin is enforced

`Broiler.ECMAScript.Generation`'s `TzdbPin` reads only the members the pin names, after the tarball's
length, SHA-256 and SHA-512 and each member's length and SHA-256 match it, and refuses a release whose
`version` member states another one. Nothing is generated from bytes that fail a check.

- `Broiler.ECMAScript.Generation.Tests` hashes the archive and every member against the pin on every
  run, lists this directory and requires nothing beside the pin but the archive, this README and the
  `zic` comparison, and **holds `tzdb.pin` byte-identical to Broiler.VM's** by its SHA-256, a constant the
  test states.
- Broiler.VM's rule `I4` holds the same equality from its side once its cut-over, milestone SJL-8.5,
  regenerates its tables with the home's generator. **The pin moves only by a dated decision that moves
  both repositories' pins in the same change** (JSD-0076, and the milestone's exit clause 13), never here
  alone.
- Rule SL7 regenerates [`../generated/`](../generated) from this archive and compares every table byte
  for byte.

**The tarball is binary in [`.gitattributes`](../../../../.gitattributes)**, so no checkout filter
rewrites a byte under a recorded hash. The pin and this README stay text.

## The `zic` comparison

[`check-zic.py`](check-zic.py) compiles the nine pinned source files with the host's `zic -b fat` and
compares every identifier's offsets, 1800 to 2037, with the generated zone table (milestone SJL-8's
data-home exit clause 3). Run it from the repository root:

```bash
python3 -I Broiler.ECMAScript/data/time/tzdb/check-zic.py
```

It needs `zic`, which a test run does not, so no test runs it: the shared-libraries workflow runs it on
Linux, whose runners have `zic`, and the run that closes a work package keeps its output in that package's
evidence bundle. It exits 1 on any difference but the four names that read their CLDR primaries by design
(`CST6CDT`, `EST5EDT`, `MST7MDT`, `PST8PDT`, before 1966-10-31).

## Updating

A later release is a new archive and a new pin, taken twice and compared, in Broiler.VM and here in the
same change; the generator then writes new tables, and the change is a dated decision recorded beside the
release's NEWS, since a release can move offsets that programs have already observed.
