# Calendar data pin

**Owner:** the Broiler.JS maintainer, as the data home's owner (decision SJL-D-1 (b-iii)). **Reviewer:** none.

The two published crates both JavaScript engines' calendar tables are generated from, archived here
unmodified. [`calendars.pin`](calendars.pin) records each crate's length, SHA-256 and SHA-512 and each
member a generator reads. Decision SJL-D-21 takes the calendar sources from these crates and from no .NET
calendar class; Broiler.VM's decision JSD-0076 (milestone SJL-8.1) records it and makes this repository
their home.

## Where they came from

**This directory is a copy of Broiler.VM's `src/tests/calendars/pins/`**, taken at Broiler.VM `9508bd5` at
work package SJL-8.2. Both crates and `calendars.pin` are byte-identical to Broiler.VM's, with identical
hashes: nothing was retrieved again, and the pin's own comment, which tells how Broiler.VM took the crates,
is kept unchanged. Only this README is rewritten. Broiler.VM's pin README records the retrieval: twice on
2026-10-05 from `https://static.crates.io/crates/`, into two directories that `cmp` found byte-identical,
the first copies archived; each crate's SHA-256 is also the `cksum` the crates.io index states for its
version.

| | |
|---|---|
| `icu_calendar` 2.3.0 | ICU4X's calendar crate, as crates.io serves it, under the Unicode License v3. The generator reads four of its files: the Chinese years 1912 to 2102 (`china_data.rs`), the Korean years 1912 to 2102 (`korea_data.rs`), the Qing-era years 1900 to 1911 both calendars share (`qing_data.rs`), and the Umm al-Qura years 1300 to 1600 AH (`ummalqura_data.rs`) |
| `calendrical_calculations` 0.2.4 | its dependency, under the Apache License 2.0. The generator reads one array of `persian.rs`: the 78 years from 1502 AP that the Persian calendar's 33-year rule makes leap and the astronomical calendar does not |
| Versions | each crate's `Cargo.toml`, which states its name and version; the pin names both |
| Licences | each crate's `LICENSE`, inside the crate; [`THIRD_PARTY_NOTICES.md`](../../../../THIRD_PARTY_NOTICES.md) carries both |

## How the pin is enforced

`Broiler.ECMAScript.Generation`'s `CalendarPin` hashes each crate and compares its length, SHA-256 and
SHA-512, then hashes every member the pin names after extraction, and refuses a crate whose `Cargo.toml`
states another name or version than the pinned one. Nothing is generated from bytes that fail a check.

- `Broiler.ECMAScript.Generation.Tests` runs those checks on every run, lists this directory and requires
  the pin to name every crate in it and nothing else, and **holds `calendars.pin` byte-identical to
  Broiler.VM's** by its SHA-256, a constant the test states.
- Broiler.VM's rule `I4` holds the same equality from its side once its cut-over, milestone SJL-8.5,
  regenerates its table with the home's generator. **Re-pinning to a later crate release is a dated
  decision that moves both repositories' pins in the same change**, never here alone.
- Rule SL7 regenerates [`../generated/calendars.bin`](../generated) from these crates and compares it byte
  for byte.

**Both crates are binary in [`.gitattributes`](../../../../.gitattributes)**, so no checkout filter
rewrites a byte under a recorded hash. The pin and this README stay text.
