# The time data home

**Owner:** the Broiler.JS maintainer (decision SJL-D-1 (b-iii)). **Reviewer:** none.

This directory is the home of both JavaScript engines' time zone and calendar data: the pinned inputs, and
the tables the shared generator writes from them. Broiler.VM's decision JSD-0076 (milestone SJL-8.1 of the
shared JavaScript libraries programme) put it here, and milestone SJL-8.2 built it. **No package carries
anything in it** (decision SJL-D-6 (c)): each engine embeds the generated tables itself, Broiler.VM in its
`.Intl` assembly and Broiler.JS in `BuiltIns`, from their cut-overs, milestones SJL-8.5 and SJL-8.6.

| Directory | Holds |
|---|---|
| [`tzdb/`](tzdb/README.md) | IANA tzdb **2026e**'s data tarball and `tzdb.pin`, copies of Broiler.VM's; and `check-zic.py`, the comparison of the generated zone table with the host's `zic` |
| [`cldr/`](cldr/README.md) | CLDR **48.2.0**'s `bcp47/timezone.json`, which gives the primary identifiers, its one-file `cldr.pin`, and the CLDR licence |
| [`calendars/`](calendars/README.md) | ICU4X's `icu_calendar` **2.3.0** and `calendrical_calculations` **0.2.4** crates and `calendars.pin`, copies of Broiler.VM's |
| `generated/` | The tables, written by the generator and never by hand, and their manifest |

## The generated tables

`Broiler.ECMAScript.Generation` writes them from the pins above. Each file is the bytes an engine embeds, in
Broiler.VM's compact layout, which `TzdbTables` and `CalendarTables` in that package state:

| File | Table |
|---|---|
| `time-zones.bin` | every Zone's offsets: a zig-zag first offset, a palette of offsets with their daylight flags, varint transitions, and the recurring rules that continue them without limit |
| `time-zone-ids.txt` | every Zone and Link name, 597 of them, with the index of the zone it reads and its CLDR primary identifier where that is another; `UTC` for the UTC family, and no `Factory` |
| `time-zone-regions.txt` | `zone.tab`'s zones of each country |
| `calendars.bin` | the Chinese, Korean and Qing years, the Umm al-Qura years, and the Persian corrections |
| `tables.manifest` | the versions, and each table's length and SHA-256 |

The text tables are ASCII, their lines separated by LF and the last one unterminated, so that each file's
bytes are the table's. They are byte for byte the data Broiler.VM's `JsTzdbTables.g.cs` and
`JsCalendarTables.g.cs` hold at Broiler.VM `9508bd5`.

## How it is held

- **Rule SL7** (`Broiler.ECMAScript.Architecture.Tests`) regenerates every file of `generated/` from the
  pins and compares it byte for byte, in both directions. To write them instead, after a change to the
  generator or a pin:

  ```bash
  BROILER_ECMASCRIPT_TIME_WRITE=1 dotnet test Broiler.ECMAScript/Broiler.ECMAScript.Architecture.Tests -c Release --filter "FullyQualifiedName~SL7_"
  ```

- **`Broiler.ECMAScript.Generation.Tests`** hashes every archive and member against its pin, holds each pin
  to Broiler.VM's, holds each table to the digest of Broiler.VM's data, and holds the source writers to
  Broiler.VM's generated files.
- **`tzdb/check-zic.py`** compares the zone table with the host's `zic -b fat` over the nine pinned files,
  1800 to 2037 (milestone SJL-8's data-home exit clause 3).
- **Broiler.JS embeds the files of `generated/` in `Broiler.JavaScript.BuiltIns`**, linked from here and never
  copied, and its `SharedTimeZoneTests` holds each embedded file to this directory's bytes and to the manifest's
  digests. *(Added 2026-10-17, at milestone SJL-8.6.)*

A table never changes alone: a change to one is a change of a pin, or of the generator, recorded with the
decision that makes it, in both repositories.
