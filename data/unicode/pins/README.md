# Unicode data pin

**Owner:** the Broiler.JS maintainer. **Reviewer:** none.

The Unicode Character Database files, the auxiliary break data and its tests, the two emoji sequence
files, the Unicode licence text and ECMA-262's three property tables that this engine's embedded Unicode
tables are generated from, archived here unmodified. [`unicode.pin`](unicode.pin) records each file's
length and SHA-256.

## Where they came from

**This archive is a copy of Broiler.Unicode's**, `data/unicode/pins/` in that repository, taken on
2026-10-16 at work package SJL-7.5 of the shared JavaScript libraries programme, at Broiler.Unicode
`6e14f93`, the commit its `0.1.0-preview.4` packages were published from. Decision SJL-D-6 (c), as
Broiler.VM's JSD-0074 applies it, says each engine "embeds the tables the owner's generator writes from
that engine's own pin". Every file here, `unicode.pin` included, is byte-identical to the owner's; nothing
was retrieved again. The owner's archive is itself a copy of Broiler.VM's `src/tests/unicode/pins/`,
taken byte for byte at Broiler.VM `7b44dc7`, and Broiler.VM's pin and its README record how each file was
retrieved from unicode.org and tc39/ecma262, twice and byte-identical, between 2026-09-22 and 2026-10-04.
`unicode.pin`'s own comment, which this copy keeps unchanged, states the step from Broiler.VM to the
owner.

Two files of the owner's directory are not copied: its README, which this file replaces, and
`broiler-vm/`, the owner's record of Broiler.VM's pin, which no test here reads.

| | |
|---|---|
| Version | **Unicode 17.0.0** |
| UCD files | under [`ucd-17.0.0/`](ucd-17.0.0) at their UCD paths, `auxiliary/`, `emoji/` and `extracted/` kept |
| Emoji sequence files | under [`emoji-17.0.0/`](emoji-17.0.0), from the release's `emoji/` directory |
| ECMAScript tables | under [`ecma262/`](ecma262), from tc39/ecma262 at `0248456c`; the pin names them by path from the repository root, which is why this copy sits at the same path as the owner's |
| Test-only | `NormalizationTest.txt`, `GraphemeBreakTest.txt`, `WordBreakTest.txt` and `SentenceBreakTest.txt`: no table is generated from them |

## How the pin is enforced

`Broiler.JS/Broiler.JavaScript.Ast.Tests/UnicodeTableRegenerationTests.cs` runs the owner's generator,
the test-only package `Broiler.Unicode.Generation`, over this archive and holds the embedded tables,
`Broiler.JS/Broiler.JavaScript.Ast/Unicode/JSUnicodeTableData.g.cs`, equal to its output byte for byte
(the programme's rule SL7). The generator refuses any file whose length, SHA-256 or version header is not
what the pin records. The same test holds this pin equal to the one the generator package carries, which
is the owner's, entry by entry (the shape of Broiler.VM's rule I4). **The pin moves only by a dated
decision that moves the owner's and Broiler.VM's in the same change**, never here alone.

*Revised 2026-10-21, at milestone SJL-12.5, when the tables and their test moved, unchanged, from
`Broiler.JavaScript.Runtime` and its tests to `Broiler.JavaScript.Ast` and its tests: the paragraph's two
paths read `Broiler.JS/Broiler.JavaScript.Runtime.Tests/UnicodeTableRegenerationTests.cs` and
`Broiler.JS/Broiler.JavaScript.Runtime/Unicode/JSUnicodeTableData.g.cs`.*

**Every archived file is binary in [`.gitattributes`](../../../.gitattributes)**, so no checkout filter
rewrites a line ending and moves a hash on one platform.

## Licence

The Unicode files are © Unicode, Inc., under the Unicode License v3 (SPDX `Unicode-3.0`), whose text is
retained as [`unicode-LICENSE.txt`](unicode-LICENSE.txt) and reproduced in
[`THIRD_PARTY_NOTICES.md`](../../../THIRD_PARTY_NOTICES.md). The ECMAScript tables are © Ecma
International, from the ECMA-262 specification's source, under the Ecma alternative copyright notice, whose
full text is retained beside them at
[`ecma262/ECMA-alternative-copyright-notice.txt`](ecma262/ECMA-alternative-copyright-notice.txt) with the
notice of changes it asks for. That file is not pinned: the generator never reads it. **The files are
unmodified**, which their hashes attest.
