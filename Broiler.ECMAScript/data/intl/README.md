# The Intl data home

**Owner:** the Broiler.JS maintainer (decision SJL-D-1 (b-iii)). **Reviewer:** none.

This directory is the home of both JavaScript engines' locale data: the pinned inputs, the locale list of each
embedding, and the tables the shared generator writes from them. Broiler.VM's decision JSD-0082 (milestone SJL-9.1
of the shared JavaScript libraries programme) put it here under decision SJL-D-22 (a), and work package SJL-9.2b
built it. **No package carries anything in it** (decision SJL-D-6 (c)): each engine embeds its own tables -
Broiler.VM as its `JsCldrTables.g.cs`, which the generator writes with Broiler.VM's options, and Broiler.JS the
files of `generated/`, from its cut-over, milestone SJL-9.5. Until then nothing in Broiler.JS reads this directory.

| Directory | Holds |
|---|---|
| [`cldr/`](cldr/README.md) | CLDR **48.2.0**'s files and `cldr.pin`: every file Broiler.VM's pin names, byte-identical, and Broiler.JS's extra main-data files from the same packages (decision D10); the CLDR licence |
| [`ucd/`](ucd/README.md) | The Unicode Character Database **17.0.0**'s `UnicodeData.txt` and `PropList.txt` and `ucd.pin`, held equal to Broiler.Unicode's pin entries (decision D12); the Unicode licence |
| `locales/` | Each embedding's locale list (decision D5): [`broiler-vm.txt`](locales/broiler-vm.txt), Broiler.VM's, which does not grow, and [`broiler-js.txt`](locales/broiler-js.txt), Broiler.JS's |
| `generated/` | Broiler.JS's tables, one file per table, written by the generator and never by hand, and their manifest |

## The generator and its parameters

`Broiler.ECMAScript.Generation`'s `Intl` namespace is Broiler.VM's `CldrTableGenerator`, `UnicodeDatabase` and
`CldrPin` at Broiler.VM `4cf9ca44`, ported (decision D11; [the design](../../docs/intl/design.md), section 4):

| Type | What it is |
|---|---|
| `IntlCldrPin`, `IntlUcdPin` | The two pins' readers and checks, the UCA version tied to the Unicode version |
| `CldrUcd` | The subset of the UCD the generator reads |
| `IntlLocaleList`, `IntlGeneratorOptions` | The parameters: the locales the data supports, the main-data locales, the supplemental languages, which numbering systems' symbols the number table carries, the calendars and the tailorings |
| `CldrTableGenerator` | The tables, Broiler.VM's, collation included |
| `IntlTableSource` | The tables as C#, with `TableSourceOptions`, as the time tables' writers: with Broiler.VM's options, Broiler.VM's `JsCldrTables.g.cs` byte for byte before its assurance pass |
| `IntlTableFiles` | The tables as raw files, one per table, and `tables.manifest` |
| `IntlDataVersions` | CLDR 48.2.0, UCA 17.0.0 and Unicode 17.0.0 |

**The two embeddings differ only in their parameters.** Broiler.VM's list carries `de`, `de-DE`, `en`, `en-US` and
`und`, with main data and supplemental rules for `de` and `en`, and the `latn` symbols alone. Broiler.JS's list
carries the locales decision D5 gives it, with main data for each that CLDR names among its `full` locales, `und`
included; its plural, ordinal, plural-range and day-period tables cover every language CLDR states rules for; its
number table carries each numbering system's own symbols (decision D21) - those a locale's `numbers.json` states, as
keys `symbols@<system>.<name>`, and root's `arab` and `arabext` symbols from `common/main/root.xml`, under `und` -
and a system no locale on the parent chain states is written in the locale's `latn` symbols, as root's aliases say.
Both carry the same global tables (likely subtags, aliases, extensions, currency digits, numbering systems,
metazones, hour cycles, weeks, scripts and collation).

## The generated tables

`generated/` holds Broiler.JS's tables, each the bytes Broiler.JS will embed, in Broiler.VM's format, which its
`JsIntlTable` documents: the text tables ASCII lines of fields separated by `|`, LF between lines and the last one
unterminated, a character a line cannot carry written `\uXXXX`; the collation tables binary in the layouts
Broiler.VM's `JsCollationData` reads. A table's file is named for it - `LikelySubtags` is `likely-subtags.txt`,
`CollationRoot` `collation-root.bin` - and `tables.manifest` names the data versions and each file's length and
SHA-256. The format is versioned by this home: a change to it is a major version of `Broiler.ECMAScript.Intl`
(the design's section 3).

## How it is held

- **Rule SL7** (`Broiler.ECMAScript.Architecture.Tests`) regenerates every file of `generated/` from the pins and
  Broiler.JS's list and compares it byte for byte, in both directions. To write them instead, after a change to
  the generator, a pin or the list:

  ```bash
  BROILER_ECMASCRIPT_INTL_WRITE=1 dotnet test Broiler.ECMAScript/Broiler.ECMAScript.Architecture.Tests -c Release --filter "FullyQualifiedName~SL7_"
  ```

- **`Broiler.ECMAScript.Generation.Tests`** holds:
  - the pins (`IntlPinTests`): the archive to its pin; Broiler.VM's lines in the pin to their SHA-256 at
    Broiler.VM `4cf9ca44`; the `timezone.json` entry and copy to the time home's; the UCD copies to
    Broiler.Unicode's pin entries; each with a control in which one hash is changed;
  - **byte identity with Broiler.VM** (`IntlTableSourceTests`): with Broiler.VM's options, over the home's copy of
    Broiler.VM's pinned files and the UCD copies, the source writer's text has the SHA-256 of Broiler.VM's
    `JsCldrTables.g.cs` at Broiler.VM `4cf9ca44` before its assurance pass, a constant the test states; and two
    runs and two threads write the same bytes;
  - **completeness** (`IntlCompletenessTests`), the milestone's exit clause 3 per embedding: every locale of each
    list has every table its group carries, with decision D34's clock keys for every locale of Broiler.JS's list.
- **Broiler.VM's rules N27, N28 and I4** hold its own copy and table, from its work package SJL-9.2c.

A table never changes alone: a change to one is a change of a pin, the generator or a list, recorded with the
decision that makes it, in both repositories. An addition to either list is a dated data decision with its
measured size (decision D5).
