# CLDR data pin of the Intl data home

**Owner:** the Broiler.JS maintainer, as the data home's owner (decision SJL-D-1 (b-iii)). **Reviewer:** none.

The files of the Unicode Common Locale Data Repository the Intl data home's generator reads, and the CLDR
licence text, archived here unmodified. [`cldr.pin`](cldr.pin) records each file's length and SHA-256.
Broiler.VM's decision JSD-0082 (milestone SJL-9.1 of the shared JavaScript libraries programme) makes this pin
a **superset of Broiler.VM's** `src/tests/cldr/pins/cldr.pin` (decision D10): every line of that pin from
`data` to its last `file` line is in this one, unchanged and in its order, and the `file` lines after them name
Broiler.JS's extra files: main-data files from the same npm packages at the same version, and CLDR's `common/main`
XML files that decision D21 reads, from the CLDR repository at the tag Broiler.VM's `common/main/root.xml` came from.

## Where it came from

**Broiler.VM's files are copies of Broiler.VM's**, taken at Broiler.VM
`4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f` (the merge that carries its SJL-9 rules) from its
`src/tests/cldr/pins/`, at the same paths below the pin. They are byte-identical to Broiler.VM's, with identical
hashes; none was retrieved again. Broiler.VM's pin records how they were taken.

**Broiler.JS's extra main-data files** were added on 2026-10-26 at work package SJL-9.2b. Broiler.JS's
embedding (decision D5, [`../locales/broiler-js.txt`](../locales/broiler-js.txt)) reads the main data of every
locale of its list that cldr-core's `availableLocales.json` names among its `full` locales, and for each, the
files Broiler.VM's generator reads for `de` and `en`: `numbers.json` and `currencies.json`, `units.json`,
`listPatterns.json`, the five names files of `cldr-localenames-full`, `ca-generic.json`, `ca-gregorian.json`,
`dateFields.json` and `timeZoneNames.json`, and the `ca-*.json` files of the calendars Broiler.VM's generator
reads. The tarballs of the packages they come from were retrieved twice from the npm registry, into two
directories that `cmp` found byte-identical, and each was checked against the sha512 integrity its
`json-integrity` line states (Broiler.VM's lines, unchanged) before any file was read from it. Each of Broiler.VM's
npm files from those packages was found byte-identical to its member of the same tarballs, and each of its files from
`cldr-core` and `cldr-bcp47`, which the extra files do not come from, to its member of those packages' tarballs,
retrieved twice on 2026-10-26 for that comparison alone, each pair byte-identical and each checked against its
`json-integrity` line. None of Broiler.VM's files was taken from these retrievals.

**Broiler.JS's `common/main` XML files** were added on 2026-10-26 for decision D21: Broiler.JS's number table carries
each numbering system's own symbols for every locale of its list, through the locale's parent chain to root, and a
cldr-json `numbers.json` carries the symbols of a locale's default, native, traditional and finance systems only. The
generator reads the other systems' symbols from `common/main/<locale>.xml` for every main-data locale of the list but
`und` (whose file is Broiler.VM's `root.xml`) and for every locale on their parent chains below root, by
`parentLocales.json` or by truncation, which adds `en_001.xml`. They were retrieved twice from
`https://raw.githubusercontent.com/unicode-org/cldr/release-48-2/common/main/`, the pin's `repository-source`, into two
directories found byte-identical, and the `root.xml` retrieved with them equals Broiler.VM's. They state no version and
are held by their hashes alone.

| | |
|---|---|
| Version | **CLDR 48.2.0**; UCA **17.0.0**, the Unicode version of [`../ucd/`](../ucd/README.md) |
| Files | `cldr-48.2.0/common/` (Broiler.VM's collation files, `common/main/root.xml` and the UCA files; Broiler.JS's `common/main/<locale>.xml` files) and `cldr-48.2.0/json/<package>/` (the cldr-json files), each at its path in its source |
| Licence | [`cldr-LICENSE.txt`](cldr-LICENSE.txt), the CLDR repository's `LICENSE` at the tag `release-48-2`: the Unicode License v3 (SPDX `Unicode-3.0`) |
| Retrieved | Broiler.VM's files by Broiler.VM, 2026-10-04 and 2026-10-05; the extra files and the XML files 2026-10-26; each **twice**, byte-identical |

### The names files CLDR does not carry

`cldr-localenames-full` 48.2.0 carries no file of these names for these locales, so the pin names none and the
generator reads none: the locale has no names of that kind (CLDR's root has none either). Each was found absent
from the package's tarball at retrieval, and `Broiler.ECMAScript.Generation.Tests` holds the generator's list of
absent files equal to this one, so that a file left out of the archive by mistake would be found.

| Locale | Files CLDR does not carry |
|---|---|
| `aa` | `languages.json`, `scripts.json`, `territories.json`, `variants.json` |
| `ab` | `scripts.json`, `territories.json`, `variants.json` |
| `af`, `az`, `sa`, `si`, `ur`, `uz`, `uz-Cyrl`, `yi` | `variants.json` |
| `en-Shaw` | `languages.json`, `scripts.json`, `territories.json`, `variants.json` |
| `ff`, `gv`, `ln`, `pap` | `scripts.json`, `variants.json` |
| `jbo` | `scripts.json`, `territories.json`, `variants.json` |
| `und` | `languages.json`, `scripts.json`, `territories.json`, `variants.json` |

As paths: aa/languages.json, aa/scripts.json, aa/territories.json, aa/variants.json, ab/scripts.json,
ab/territories.json, ab/variants.json, af/variants.json, az/variants.json, en-Shaw/languages.json,
en-Shaw/scripts.json, en-Shaw/territories.json, en-Shaw/variants.json, ff/scripts.json, ff/variants.json,
gv/scripts.json, gv/variants.json, jbo/scripts.json, jbo/territories.json, jbo/variants.json, ln/scripts.json,
ln/variants.json, pap/scripts.json, pap/variants.json, sa/variants.json, si/variants.json, und/languages.json,
und/scripts.json, und/territories.json, und/variants.json, ur/variants.json, uz-Cyrl/variants.json,
uz/variants.json and yi/variants.json, each below `cldr-48.2.0/json/cldr-localenames-full/main/`.

## How the pin is enforced

- **`Broiler.ECMAScript.Generation`'s `IntlCldrPin`**, a port of Broiler.VM's `CldrPin`, compares every file's
  length and SHA-256, and the version a file states where it states one, with the pin before the generator
  reads anything; it also refuses a pin whose `uca-version` is not the Unicode version the generator reads
  (decision D12).
- **`Broiler.ECMAScript.Generation.Tests`** (`IntlPinTests`) holds the archive to the pin and the pin to every
  file beside it; holds Broiler.VM's lines in this pin to their SHA-256 at Broiler.VM `4cf9ca44`, a constant the
  test states, and the home's files to those lines alone; and holds the `timezone.json` entry and copy equal to
  the time home's ([`../../time/cldr/`](../../time/cldr/README.md)), which is unchanged.
- **Broiler.VM's rule I4** holds Broiler.VM's pin to this one entry by entry, read at a recorded commit of this
  repository, from Broiler.VM's work package SJL-9.2c (decision D10).

A re-pin to a later CLDR release is a dated decision that moves both repositories in the same change, and an
addition to Broiler.JS's list is a dated data decision with its measured size (decision D5).

**The archive is binary in [`.gitattributes`](../../../../.gitattributes)**, so no checkout filter rewrites a
line ending under a recorded hash. The pin and this README stay text.

## Licence

The files are © Unicode, Inc., distributed under the Unicode License v3 (SPDX `Unicode-3.0`), whose text is
retained here as [`cldr-LICENSE.txt`](cldr-LICENSE.txt) and reproduced in
[`THIRD_PARTY_NOTICES.md`](../../../../THIRD_PARTY_NOTICES.md). **The files are unmodified**, which their hashes
attest. The generated tables in [`../generated/`](../README.md) are derived from them.
