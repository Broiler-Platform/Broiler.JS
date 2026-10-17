# CLDR time zone keys pin

**Owner:** the Broiler.JS maintainer, as the data home's owner (decision SJL-D-1 (b-iii)). **Reviewer:** none.

The one file of the Unicode Common Locale Data Repository the time data home's generator reads, and the
CLDR licence text, archived here unmodified. [`cldr.pin`](cldr.pin) records the file's length and
SHA-256. Under decision SJL-D-20 the primary identifiers are CLDR's: the identifiers one time zone key of
`bcp47/timezone.json` names are one zone, whose primary identifier is the key's `_iana` name, or its first
alias where it states none. Broiler.VM's decision JSD-0076 pins this file beside the tz archive.

## Where it came from

**Both files are copies of Broiler.VM's**, taken at Broiler.VM `9508bd5` at work package SJL-8.2, from its
`src/tests/cldr/pins/`: `cldr-48.2.0/json/cldr-bcp47/bcp47/timezone.json`, at the same path below the pin,
and `cldr-LICENSE.txt`. Both are byte-identical to Broiler.VM's, with identical hashes; nothing was
retrieved again. Broiler.VM's `cldr.pin` names 114 CLDR files, and this pin names the one of them the time
zone tables read: its `file` line is Broiler.VM's for that file, unchanged, and its other lines are
Broiler.VM's for the package the file is from.

| | |
|---|---|
| Version | **CLDR 48.2.0** |
| File | [`cldr-48.2.0/json/cldr-bcp47/bcp47/timezone.json`](cldr-48.2.0/json/cldr-bcp47/bcp47/timezone.json), 59,702 bytes, from the npm package `cldr-bcp47` at 48.2.0, its tarball checked against the registry's sha512 integrity before it was unpacked. It states no version and is held by its hash alone |
| Licence | [`cldr-LICENSE.txt`](cldr-LICENSE.txt), the CLDR repository's `LICENSE` at the tag `release-48-2`: the Unicode License v3 (SPDX `Unicode-3.0`) |
| Retrieved | by Broiler.VM, 2026-10-04, **twice**, into two directories that `diff -r` found byte-identical |

## How the pin is enforced

`Broiler.ECMAScript.Generation`'s `CldrPin` compares the file's length and SHA-256 with the pin before the
time zone table generator reads it, and the generator refuses a pin that does not name it.
`Broiler.ECMAScript.Generation.Tests` runs that check on every run, lists this directory and requires
nothing beside the pin but the file, its licence and this README, and **holds the pin's `file` line equal
to line 127 of Broiler.VM's `cldr.pin`**, a constant the test states. Re-pinning to a later CLDR release
is a dated decision that moves both repositories in the same change.

**Both files are binary in [`.gitattributes`](../../../../.gitattributes)**, so no checkout filter rewrites
a line ending under a recorded hash. The pin and this README stay text.

## Licence

The files are © Unicode, Inc., distributed under the Unicode License v3 (SPDX `Unicode-3.0`), whose text is
retained here as [`cldr-LICENSE.txt`](cldr-LICENSE.txt) and reproduced in
[`THIRD_PARTY_NOTICES.md`](../../../../THIRD_PARTY_NOTICES.md). **The files are unmodified**, which their
hashes attest. The primary identifiers in the generated identifier table are derived from them.
