# Unicode Character Database files of the Intl data home

**Owner:** the Broiler.JS maintainer, as the data home's owner (decision SJL-D-1 (b-iii)). **Reviewer:** none.

The two files of the Unicode Character Database the Intl data home's generator reads, and the Unicode licence
text, archived here unmodified. [`ucd.pin`](ucd.pin) records each file's length and SHA-256. Broiler.VM's
decision JSD-0082, decision D12, has the generator read General_Category, the canonical decompositions, the
canonical combining classes and Unified_Ideograph through a ported subset of Broiler.VM's `UnicodeDatabase`
(`Broiler.ECMAScript.Generation`'s `CldrUcd`), from these files, an explicit and pinned input: Broiler.VM hands
its own pinned copies, and this home hands these.

| | |
|---|---|
| Version | **Unicode 17.0.0**, the UCA version of [`../cldr/`](../cldr/README.md) |
| Files | `ucd-17.0.0/UnicodeData.txt` (General_Category, decompositions, combining classes) and `ucd-17.0.0/PropList.txt` (Unified_Ideograph) |
| Licence | [`unicode-LICENSE.txt`](unicode-LICENSE.txt), the Unicode License v3 (SPDX `Unicode-3.0`) |
| Retrieved | by Broiler.VM, 2026-09-22, **twice**, byte-identical; copied, not retrieved again |

## Where they came from

**Byte-for-byte copies of Broiler.VM's** `src/tests/unicode/pins/` at Broiler.VM
`4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f`, at the same paths below the pin. They are also byte-identical to this
repository's own copies under `data/unicode/pins/`.

## How the pin is enforced

- **`Broiler.ECMAScript.Generation`'s `IntlUcdPin`** compares each file's length and SHA-256, and
  `PropList.txt`'s stated version, with the pin before the generator reads them.
- **`Broiler.ECMAScript.Generation.Tests`** (`IntlPinTests`) holds each `file` line equal to the line
  Broiler.Unicode's own pin gives the same file, the owner's (decision SJL-D-6; `UnicodePin.Owner()` of the
  test-only package `Broiler.Unicode.Generation`), and the pin to every file beside it.
- **Broiler.VM's rule I4's Unicode clause** already holds Broiler.VM's copies to Broiler.Unicode's.

**The files are binary in [`.gitattributes`](../../../../.gitattributes)**. The pin and this README stay text.

## Licence

The files are © Unicode, Inc., distributed under the Unicode License v3 (SPDX `Unicode-3.0`), whose text is
retained here as [`unicode-LICENSE.txt`](unicode-LICENSE.txt) and reproduced in
[`THIRD_PARTY_NOTICES.md`](../../../../THIRD_PARTY_NOTICES.md). **The files are unmodified.**
