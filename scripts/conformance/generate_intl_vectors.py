#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Write the Intl area of the conformance corpus (revisions 27 and 28, milestone SJL-9.2, work package SJL-9.2a).

    python3 -I scripts/conformance/generate_intl_vectors.py --node <Node v26.12.0> --witness-node <Node 22.22.0>
        --cldr <directory holding the cldr-json 48.2.0 tarballs> [--out conformance/vectors/intl] [--spec <ecma402>]

THE AREA. Five files, one per sub-area of Broiler.VM's JSD-0082 (the area verdict) in the order its decision D3 takes
them: (a) locale identifiers, likely subtags, negotiation, Locale info and supportedValuesOf (locales.vectors);
(b) the Intl mathematical value, digit options and FormatNumericToString, plural rules and plural ranges
(values-and-plurals.vectors); (c) number formatting, list, relative-time and display-name formatting
(numbers-and-text.vectors); (d) date-time formatting with intervals, day periods, hour cycles, zone names and Intl over
Temporal (date-times.vectors); (e) duration formatting (durations.vectors). Every vector names, in its note, the rows
of bundle sjl-9-001's correspondence (Broiler.VM's docs/evidence/sjl-9-001/correspondence.md) whose inputs it takes;
every row with a behavioural difference is reached by at least one vector (``ROWS`` and ``check_rows``). Each file
also carries, for each locale of Broiler.JS's embedding beyond Broiler.VM's (decision D5), one vector per table family
of the shared locale data (decision D17: "one per table family for each extra locale").

THE EXPECTED VALUES (decision D17 as revised on 2026-10-26). Every expected value is ICU's, through the reference
Node.js release bundle sjl-9-001's reference.txt names, v26.12.0 (ICU 78.3, CLDR 48.0), driven by intlref.mjs
(``ref:intlref@1``), with three exceptions, each recorded in the vector:
- where the pinned ECMA-402 text gives another answer, the vector takes the text's, argued in its note against the
  clause its provenance names, which quotes the reference's answer, and records it as ``overruled:intlref@1=<outcome>``
  where that outcome holds no space, which an overruled: entry cannot hold (SCHEMA.md section 6) (``Case.text``);
- (D17 (b)) a date-time string carries U+202F where ICU's pattern has it. V8 writes U+0020 there, in the reference's
  `format`, `formatToParts` and range methods alike, so the positions are read from the witness, Node.js 22.22.0's
  parts, which keep U+202F (intlref.mjs --witness): a vector takes them only where the witness's answer equals the
  reference's character for character once U+202F is read as U+0020, and its note records that the reference wrote
  U+0020 there. No overruled: entry records the reference's answer, which always holds the U+0020 it overrules; a
  date-time case whose answers do not align so is refused;
- (D34) a DurationFormat `digital` line takes CLDR 48.2.0's own reading of the locale's time separator and of
  `durationUnit-type-hms` (two-digit hours where it begins `hh`), read by this generator from the cldr-json tarballs,
  each checked against its npm integrity (``ref:intlcldr@1``), and records ICU's one-digit hours as overruled.
The `class` is `normative` where the pinned text and the locale data both engines adopt (CLDR 48, decisions D5 and
D10) fix the answer, and `impl-defined` only where the text leaves the answer to the implementation, with each engine's
expectation from its plan: the available locales (decision D5: Broiler.VM's list stays de, de-DE, en and en-US, so its
expectation is the reference's answer for the locale it resolves to, ``vm_script``), the default locale and the
other engine policies (decision D7), each engine's Collator's collations (decision D33 (c) and row L40), the
implementation-defined rows, whose answer the shared library takes from Broiler.VM, since every one keeps this answer
under Broiler.VM's rule, which admits no C3 (decision D35; row N5b's is Broiler.VM's composition, read by this
generator from CLDR 48.2.0, ``vm_unit_answer``), decision D15's names and decision D21's numbering-system symbols
(``d21_script``). A row's own script, or a table of the plan's rule, states each plan's expectation where it is
computed; no expected value is read from either engine's run. Each engine's first run is classified in scripts/conformance/intl-divergences.txt, which this generator reads
(corpus revision 28).

THE LOCALES. Broiler.JS's main-data list is decision D5's union, read here from bundle sjl-9-001's committed
locales/union.tsv and CLDR 48.2.0's availableLocales.json and defaultContent.json: the languages its CLDR package
serves, every language and every CLDR locale the passing intl402 files and the datasets name, en-US and und. The
extra locales are that list without Broiler.VM's de, de-DE, en, en-US and und.

The generator is deterministic: the same references and inputs write the same bytes (its tests regenerate the area
where the references are present).
"""

from __future__ import annotations

import argparse
import base64
from dataclasses import dataclass, field
import hashlib
import json
import math
import os
from pathlib import Path
import re
import struct
import subprocess
import sys
import tarfile
import tempfile
from typing import Callable, Iterable, Sequence

SCRIPTS = Path(__file__).resolve().parent
REPOSITORY = SCRIPTS.parents[1]
CORPUS = REPOSITORY / "conformance" / "vectors"
DEFAULT_OUT = CORPUS / "intl"
INTLREF = SCRIPTS / "intlref.mjs"
UNION = REPOSITORY / "docs" / "evidence" / "sjl-9-001" / "locales" / "union.tsv"
SINCE = 27              # the corpus revision that added the area
CORPUS_REVISION = 29    # the corpus revision this generator writes: REVISIONS.md's last row
GENERATOR = "generate_intl_vectors.py"
DIVERGENCES = SCRIPTS / "intl-divergences.txt"

ECMA402 = "spec:ecma402@3d4c558#"
TEMPORAL = "proposal:temporal@e8cc03fc#"
ERA = "proposal:intl-era-monthcode@5833eae#"
REF = "ref:intlref@1"
REF_CLDR = "ref:intlcldr@1"
OVERRULED = "overruled:intlref@1="

# The cldr-json 48.2.0 tarballs this generator reads, each by the sha512 integrity the npm registry publishes, which
# Broiler.VM's src/tests/cldr/pins/cldr.pin records as its json-integrity lines.
CLDR_TARBALLS = {
    "cldr-core": "sha512-zfmLothncSwfv2jlevoSrgI2VGgH8SDGHXst6jUEotHA8nq9Igeg9jzdJDFtBso9pgJuG89DK16TzrmZDdo2Bg==",
    "cldr-numbers-full": "sha512-0EQ+UkVDsyXOlxnWnL1RIENVZFqCnb04D1Yrku0obnOyttbuZz8tHmUd5c5AEvhGS/Gg2zI4fTaCm51LSGITog==",
    "cldr-units-full": "sha512-BjDC7V46eeXzgalnIumrJ1XQDk8wJaOxNLME4hmkBT9Q0hnzxn3XgtavigPUwnljk7FxlNQW9Wx+hbFrARPrQg==",
}

FILES = {
    "a": ("locales", "(a) locale identifiers, likely subtags, negotiation, Locale info and supportedValuesOf"),
    "b": ("values-and-plurals", "(b) the Intl mathematical value, digit options and FormatNumericToString; plural rules "
                                "and plural ranges"),
    "c": ("numbers-and-text", "(c) number formatting; list, relative-time and display-name formatting"),
    "d": ("date-times", "(d) date-time formatting, intervals, day periods, hour cycles, zone names and Intl over "
                        "Temporal"),
    "e": ("durations", "(e) duration formatting"),
}

VM_LOCALES = ("de", "de-DE", "en", "en-US", "und")

# Decision D5 (and rows L28 and T16): Broiler.VM's locale list stays de, de-DE, en and en-US (und is no service's
# locale), and its default locale is en-US (decision D7). A service it is asked for another locale resolves, by
# ResolveLocale's LookupMatcher over that list, to the list's best available locale or to the default, and answers
# for that locale. ECMA-402 leaves [[AvailableLocales]] to the implementation, so a vector whose answer moves with
# that resolution is impl-defined: Broiler.JS's expectation is the vector's out (its embedding carries the locale,
# D5), and Broiler.VM's is the reference's answer to the same script with each such locale replaced by its
# resolution under Broiler.VM's list, which is that engine's plan stated as a script.
VM_SERVICE_LOCALES = ("de", "de-DE", "en", "en-US")
VM_DEFAULT_LOCALE = "en-US"
SERVICE_LOCALE = re.compile(r'(new Intl\.(?:NumberFormat|DateTimeFormat|PluralRules|ListFormat|RelativeTimeFormat|'
                            r'DisplayNames|DurationFormat)\(|\.toLocale(?:Date|Time)?String\()"([^"\\]*)"')
WELL_FORMED_TAG = re.compile(r"^[A-Za-z]{2,3}(?:-[A-Za-z0-9]{1,8})*$")
VM_LIST_REASON = ("decision D5: Broiler.VM's locale list stays de, de-DE, en and en-US, so it resolves {moves} "
                  "(LookupMatcher over its list, rows L28 and T16) and its expectation is the reference's answer "
                  "for that locale; Broiler.JS's embedding carries the locale, so its expectation is out")

# Decision D33 (c) and row L40: each engine's Intl.Collator keeps its own data until SJL-17, and the shared
# CollationsOfLocale reads the list of the collations that engine's Collator resolves. Broiler.JS's Collator keeps its
# host path: it resolves emoji and eor for every locale and, for a language, the tailorings its NetCollationSuffix
# table maps (Broiler.JS/Broiler.JavaScript.BuiltIns/Intl/JSIntl.cs at the rule commit, kept in step with its
# SupportedCollationsSorted, which Intl.supportedValuesOf("collation") lists). Broiler.VM's offers phonebk for German
# alone (JSD-0043).
BROILER_JS_ROOT_COLLATIONS = ("emoji", "eor")
BROILER_JS_TAILORINGS = {"de": ("phonebk",), "hu": ("trad",), "ja": ("unihan",), "ko": ("unihan",),
                         "zh": ("phonetic", "pinyin", "stroke", "trad", "unihan", "zhuyin")}


def broiler_js_collations(tag: str | None = None) -> list[str]:
    """The collations Broiler.JS's Intl.Collator resolves for a locale, sorted in code-unit order, without the
    default; with no locale, every collation it lists (Intl.supportedValuesOf("collation"))."""
    if tag is None:
        names = set(BROILER_JS_ROOT_COLLATIONS).union(*BROILER_JS_TAILORINGS.values())
    else:
        names = set(BROILER_JS_ROOT_COLLATIONS).union(BROILER_JS_TAILORINGS.get(tag.split("-")[0].lower(), ()))
    return sorted(names)


def json_list(values: Sequence[str]) -> str:
    """The rendered outcome of a script that answers JSON.stringify of a list of Strings."""
    return "value: str:" + quote(json.dumps(list(values), separators=(",", ":")))


# Decision D21: Broiler.VM's limitation stays, the latn symbols for every numbering system, while Broiler.JS's
# embedding carries each system's own CLDR symbols. Where a vector names a system with its own digits, Broiler.VM's
# expectation is the reference's answer with the latn system, its ASCII digits replaced by the system's.
D21_ROWS = {"N8a", "U16", "T37"}
D21_SYSTEM = re.compile(r'-u-nu-([a-z]+)|numberingSystem: "([a-z]+)"')
D21_REASON = ("decision D21: Broiler.VM keeps the latn symbols with the system's digits (its expectation is the "
              "reference's latn answer with each ASCII digit replaced by the system's), and Broiler.JS's embedding "
              "carries the system's own CLDR symbols (its expectation is out)")


def d21_script(script: str) -> str | None:
    """Broiler.VM's answer under decision D21, stated as a script; None where the script names no digit system."""
    if "resolvedOptions" in script:
        return None
    option = re.search(r'numberingSystem: "([a-z]+)"', script)
    keyword = re.search(r"-u-nu-([a-z]+)", script)
    system = option.group(1) if option else keyword.group(1) if keyword else None
    if system in (None, "latn"):
        return None
    latn = script.replace(f"-u-nu-{system}", "-u-nu-latn").replace(f'numberingSystem: "{system}"', 'numberingSystem: "latn"')
    return ("(function (s) { var d = []; for (var i = 0; i < 10; i++) d.push(new Intl.NumberFormat("
            f'"en-u-nu-{system}").format(i)); return typeof s === "string" ? s.replace(/[0-9]/g, function (c) '
            f"{{ return d[c]; }}) : s; }})({latn})")


def vm_resolution(tag: str) -> str:
    """The locale ResolveLocale's LookupMatcher finds for a tag among Broiler.VM's list, with its Unicode extension
    kept for the service to read; the default locale where none is found (BestAvailableLocale, ECMA-402)."""
    base, sep, extension = tag.partition("-u-")
    candidate = base
    while candidate:
        for available in VM_SERVICE_LOCALES:
            if candidate.lower() == available.lower():
                return available + (sep + extension if sep else "")
        cut = candidate.rfind("-")
        if cut < 0:
            break
        if cut >= 2 and candidate[cut - 2] == "-":
            cut -= 2
        candidate = candidate[:cut]
    # LookupMatcher keeps the request's Unicode extension only with a match; the default locale takes none.
    return VM_DEFAULT_LOCALE


def vm_script(script: str) -> tuple[str, list[str]]:
    """The script with each well-formed service locale outside Broiler.VM's list replaced by its resolution, and the
    replacements, as `tag to resolved`."""
    moves: list[str] = []

    def replace(match: re.Match) -> str:
        tag = match.group(2)
        if not WELL_FORMED_TAG.match(tag.split("-u-")[0]) or "\\" in tag:
            return match.group(0)
        resolved = vm_resolution(tag)
        if resolved == tag:
            return match.group(0)
        moves.append(f"{tag} to {resolved}")
        return f'{match.group(1)}"{resolved}"'

    return SERVICE_LOCALE.sub(replace, script), moves
PACKAGE_LANGUAGES = ("ar", "de", "en", "es", "fr", "it", "ja", "ko", "nl", "pl", "pt", "ru", "sv", "tr", "zh")

# Every row of the correspondence, by its class in JSD-0082's section "Every row, classified". A row is behavioural
# when its class is neither "same" nor "unreached" (code no input reaches); every behavioural row must be reached.
ROWS = {}
for _cls, _names in {
    "same": "L1 L3 L4 L11 L13 L16 L23 L27 L31 L34 L36 L38 L39 L41 M2a M6 M8 M10 P5c P8 N1a N2a N3a N5a N7a N8a N9f N13 "
            "N17a S1a S2 S3 S4a S4c S6 S7 S10 T1 T6 T8 T9 T21 T25 T28 T32 T38 U4 U5 U6 U8 U10 U11 U12 U17 U18 U20",
    "correction-js": "L2 L10 L12 L14 L15 L17 L20 L21 L25 L29 L30 L37 L44 L45 M1a M1b M2b M3 M4 M5 M7 P1 P2 P5a P6 P7 "
                     "N4 N6a N6b N6c N6e N6f N9b N10 N11 S5 S9 T2 T4 T7 T14 T19 T27 T29 T34 T35 T39 U1 U2 U3 U7 U9",
    "correction-vm": "L9 L18 Z8",
    "impl-defined": "P5b N2c N5b N5c N9a N9d N12 N17c T5 T13 T18 T20 T23 T26 T30 T33",
    "data": "L5 L6 L7 L8 L19 L22 L24 L26 L32 P3 N1b N2b N3b N3c N5d N6d N7b N8b N9c N9e N14 S1b S4b S8 T10 T12 T15 T16 "
            "T17 T22 T24 T31 T36 T37 U13 U14 U15 U16",
    "engine-policy": "L28 L33 L35 L40 L43 M9 N16 T3 T11 T40 U19",
    "unreached": "L42 P4 N15 N17b T41 T42 U21 U22",
}.items():
    for _row in _names.split():
        ROWS[_row] = _cls
BEHAVIOURAL = tuple(row for row, cls in ROWS.items() if cls not in ("same", "unreached"))

# Rows whose difference no vector can observe: the charges (decision D18), which an engine-level runner does not
# measure. Their vectors take the charged paths; the charges are held by Broiler.VM's rule's fuel parts.
CHARGE_ROWS = ("L43", "M9", "N16", "T40", "U19")


class GeneratorError(RuntimeError):
    """The generator cannot write the area, and says why."""


# ---- Typed values and scripts ------------------------------------------------------------------------------------


@dataclass(frozen=True)
class Big:
    """A BigInt input."""
    value: int


def f64_bits(x: float) -> tuple[int, int]:
    raw = struct.unpack(">Q", struct.pack(">d", x))[0]
    if math.isnan(x):
        raw = 0x7FF8000000000000
    return raw >> 32, raw & 0xFFFFFFFF


def typed(value) -> str:
    """A Python value as the schema's typed value (section 3)."""
    if value is None:
        return "undefined"
    if isinstance(value, bool):
        return "bool:true" if value else "bool:false"
    if isinstance(value, Big):
        return f"bigint:{value.value}"
    if isinstance(value, (int, float)):
        hi, lo = f64_bits(float(value))
        return f"f64:0x{hi:08X}{lo:08X}"
    if isinstance(value, str):
        return "str:" + quote(value)
    raise GeneratorError(f"no typed value for {value!r}")


def quote(text: str) -> str:
    """A String in the schema's notation: printable ASCII but `"` and `\\`, and \\uXXXX for every other code unit."""
    out = ['"']
    for unit in utf16_units(text):
        if unit == 0x22:
            out.append('\\"')
        elif unit == 0x5C:
            out.append("\\\\")
        elif 0x20 <= unit <= 0x7E:
            out.append(chr(unit))
        else:
            out.append(f"\\u{unit:04X}")
    out.append('"')
    return "".join(out)


def utf16_units(text: str) -> list[int]:
    data = text.encode("utf-16-le", errors="surrogatepass")
    return [data[i] | (data[i + 1] << 8) for i in range(0, len(data), 2)]


def js(text: str) -> str:
    """A JavaScript string literal, ASCII only: every code unit outside printable ASCII as \\uXXXX."""
    out = ['"']
    for unit in utf16_units(text):
        if unit == 0x22:
            out.append('\\"')
        elif unit == 0x5C:
            out.append("\\\\")
        elif 0x20 <= unit <= 0x7E:
            out.append(chr(unit))
        else:
            out.append(f"\\u{unit:04x}")
    out.append('"')
    return "".join(out)


def num(x: float) -> str:
    """A Number as script text: a safe integer as itself, any other value through the prelude's $f64, so that no
    vector's input passes through an engine's decimal parsing."""
    if isinstance(x, int) and abs(x) <= 2**53 and not (x == 0 and math.copysign(1, x) < 0):
        return str(x)
    if isinstance(x, float) and x.is_integer() and abs(x) <= 2**53 and math.copysign(1, x) > 0:
        return str(int(x))
    hi, lo = f64_bits(float(x))
    return f"$f64(0x{hi:08x}, 0x{lo:08x})"


# ---- Cases --------------------------------------------------------------------------------------------------------


@dataclass
class Case:
    """One vector before its outcome is known."""
    sub: str
    group: str
    op: str
    script: str
    inputs: tuple
    rows: tuple[str, ...]
    clauses: tuple[str, ...]
    note: str
    text: str | None = None          # the pinned text's outcome, where it overrules the reference
    text_reason: str = ""
    per_engine: dict[str, str] = field(default_factory=dict)  # impl-defined: each engine's plan's outcome
    per_engine_ref: dict[str, str] = field(default_factory=dict)  # or the reference's answer of a script that
    #                                                               states the plan's rule
    per_engine_same: bool = False    # impl-defined: both engines' plans take the library's answer, the reference's
    impl_reason: str = ""
    witness: bool = False            # a date-time string: U+202F from the witness (D17 (b))
    cldr: str | None = None          # D34: the expected outcome read from CLDR 48.2.0 by this generator
    extra_prov: tuple[str, ...] = ()
    vm_moves: tuple[str, ...] = ()   # D5: the service locales Broiler.VM resolves otherwise, filled by generate()
    text_ref: str | None = None      # a script whose reference answer is the pinned text's answer for this one
    text_fn: Callable[[str], str] | None = None  # the pinned text's answer, from the reference's
    collapse_same: bool = False      # per-engine expectations that all equal out leave the vector normative


CASES: list[Case] = []


def add(sub: str, group: str, op: str, script: str, inputs: Iterable, rows: str, clauses: Iterable[str], note: str,
        **kwargs) -> Case:
    case = Case(sub, group, op, script, tuple(inputs), tuple(rows.split()), tuple(clauses), note, **kwargs)
    for row in case.rows:
        if row not in ROWS:
            raise GeneratorError(f"{script}: row {row} is not a row of the correspondence")
    # One vector per script. A table family's vector for an extra locale (family_cases, which runs last) is not
    # written again where a row's own vector already takes the same script; that vector stands for the family.
    earlier = next((c for c in CASES if c.script == script), None)
    if earlier is not None:
        if group == "families":
            return earlier
        raise GeneratorError(f"{script}: vectored twice")
    CASES.append(case)
    return case


def E(*ids: str) -> tuple[str, ...]:
    return tuple(ECMA402 + i for i in ids)


def P(*ids: str) -> tuple[str, ...]:
    return tuple(TEMPORAL + i for i in ids)


def R(*ids: str) -> tuple[str, ...]:
    return tuple(ERA + i for i in ids)


def j(expr: str) -> str:
    """A script whose value is the JSON text of an expression, so an array or a record is compared as a String."""
    return f"JSON.stringify({expr})"


# ---- (a) Locale identifiers, likely subtags, negotiation, Locale info and supportedValuesOf ------------------------

ENGINE_POLICY_D7 = ("Decision D7: each engine's default-locale and available-locale providers are its own policy; the "
                    "expectation is each engine's plan's (Broiler.VM: en-US and its data's locales; Broiler.JS: the "
                    "host's culture, en-US when it is empty or invariant, as under the engine-level runner, which "
                    "removes LANG)")


TW_REASON = ("CLDR 48.2.0's supplemental aliases.json (cldr-core, the data both engines adopt) replaces the language "
             "tw by ak (reason macrolanguage), and CanonicalizeUnicodeLocaleId applies UTS #35's canonicalization "
             "over that data; the generator checks the alias in the tarball")
KA_YES_REASON = ("CLDR 48.2.0's bcp47/collation.json gives yes as an alias of true only for the boolean keys (kn, kb, "
                 "kc, kh, kk); ka's types are noignore and shifted, so UTS #35's canonicalization keeps ka-yes as "
                 "written")


def cases_a() -> None:
    gcl = "Intl.getCanonicalLocales"
    # L1 (same): well-formedness of ASCII tags.
    for tag in ("", "en-", "-en", "en--US", "x", "root", "en_US", "i-klingon", "zh-min-nan", "en-a-bbb-a-ccc",
                "de-1996-1996", "en-u", "en-t", "en-x", "abcdefghi"):
        add("a", "well-formed", gcl, f"{gcl}({js(tag)})", [tag], "L1", E("sec-iswellformedlanguagetag",
            "sec-canonicalizelocalelist"), "row L1 (same): IsWellFormedLanguageTag refuses the tag, a RangeError")
    for tag in ("english", "en-u-1a-gregory", "x-private", "zh-cmn-Hans"):
        add("a", "well-formed", gcl, f"{gcl}({js(tag)}).join()", [tag], "L1",
            E("sec-iswellformedlanguagetag", "sec-canonicalizeunicodelocaleid"), "row L1 (same): a well-formed tag")
    # L2: letters outside ASCII that culture casing folds into ASCII.
    for tag in ("Ko", "en-KE", "en-u-ca-gregorK", "ſr", "ıt", "İT", "ｅｎ",
                "en\u0000"):
        add("a", "ascii", gcl, f"{gcl}({js(tag)})", [tag], "L2", E("sec-iswellformedlanguagetag", "sec-language-tags"),
            "row L2: a language tag is ASCII (unicode_locale_id's subtags are ASCII letters and digits); a letter "
            "that culture casing folds into ASCII is not one, so IsWellFormedLanguageTag refuses the tag")
    # L45: a final line feed, non-ASCII decimal digits; through three paths.
    for tag in ("en\n", "en-US\n", "de-١٢٣", "en-١٢٣", "de-DE\n"):
        for path, script in (("Intl.getCanonicalLocales", f"Intl.getCanonicalLocales({js(tag)})"),
                             ("Intl.Locale", f"new Intl.Locale({js(tag)}).toString()"),
                             ("Intl.NumberFormat", f"new Intl.NumberFormat({js(tag)}).resolvedOptions().locale")):
            add("a", "ascii", path, script, [tag], "L45 L2", E("sec-iswellformedlanguagetag",
                "sec-canonicalizelocalelist"), "rows L45 and L2: unicode_region_subtag is three ASCII digits or two "
                "letters, and nothing follows the tag, whose subtags are ASCII; the tag is refused on every path")
    # L5: language aliases (CLDR languageAlias, the generated table).
    for tag in ("tl", "aju", "aam", "swc", "aa-saaho", "und-Latn-saaho", "und-aaland", "sv-aaland", "iw", "in", "ji",
                "jw", "mo", "aar", "ces", "heb", "tw", "cmn", "drw", "sh", "cnr", "art-lojban", "zh-hakka",
                "cel-gaulish", "sgn-GR", "hy-arevmda", "ja-Latn-hepburn-heploc"):
        add("a", "aliases", gcl, f"{gcl}({js(tag)})[0]", [tag], "L5", E("sec-canonicalizeunicodelocaleid"),
            "row L5 (data, C5): CLDR's languageAlias, as UTS #35's canonicalization applies it",
            **(dict(text='value: str:"ak"', text_reason=TW_REASON) if tag == "tw" else {}))
    # L6: script aliases.
    for tag in ("und-Qaai", "en-Qaai", "und-Qaai-US"):
        add("a", "aliases", gcl, f"{gcl}({js(tag)})[0]", [tag], "L6", E("sec-canonicalizeunicodelocaleid"),
            "row L6 (data, C5): CLDR's scriptAlias")
    # L7: region aliases, a list resolved by the likely region.
    for tag in ("en-QU", "en-840", "en-276", "de-276", "en-062", "en-AN", "nl-AN", "und-172", "und-Cyrl-172",
                "und-200", "cs-200", "en-BU", "de-DD", "fr-FX", "en-UK", "pt-TP", "sr-YU", "fr-ZR", "en-554", "ru-SU",
                "ru-810", "sr-CS", "ar-NT", "ar-YD", "es-419"):
        add("a", "aliases", gcl, f"{gcl}({js(tag)})[0]", [tag], "L7", E("sec-canonicalizeunicodelocaleid"),
            "row L7 (data, C5): CLDR's territoryAlias; a replacement list resolves by the likely region")
    # L8: BCP 47 type aliases in -u- and -t-.
    for tag in ("en-u-tz-usnavajo", "en-u-tz-aqams", "en-t-d0-name", "en-u-ca-islamicc", "en-u-ca-ethiopic-amete-alem",
                "en-u-ks-primary", "en-u-ms-imperial", "en-u-tz-cnckg", "en-u-tz-eire", "en-u-tz-gmt0",
                "en-u-rg-no23", "en-u-sd-cn11", "en-u-kn-yes", "en-t-m0-names", "en-u-ka-yes", "en-u-co-standard"):
        add("a", "aliases", gcl, f"{gcl}({js(tag)})[0]", [tag], "L8", E("sec-canonicalizeunicodelocaleid",
            "sec-unicode-extension-components"), "row L8 (data, C5): CLDR's BCP 47 aliases of -u- and -t- values",
            **(dict(text='value: str:"en-u-ka-yes"', text_reason=KA_YES_REASON) if tag == "en-u-ka-yes" else {}))
    # L9: a subdivision alias whose replacement is a region.
    for tag in ("en-u-sd-fi01", "en-u-rg-fi01", "en-u-rg-cn71", "zh-u-sd-cn91", "fr-u-sd-frgp", "fr-u-rg-frgua"):
        add("a", "subdivision", gcl, f"{gcl}({js(tag)})[0]", [tag], "L9 L8", E("sec-canonicalizeunicodelocaleid"),
            "rows L9 (a correction of Broiler.VM, decision D32) and L8 (data, C5): UTS #35 Annex C writes a "
            "subdivision alias of CLDR's whose replacement is a region as the region in lower case followed by zzzz")
        add("a", "subdivision", gcl, f"{gcl}({gcl}({js(tag)})[0])[0]", [tag], "L9 L8",
            E("sec-canonicalizeunicodelocaleid"), "rows L9 and L8: the canonical tag canonicalizes to itself")
        add("a", "subdivision", "Intl.Locale.prototype.toString", f"new Intl.Locale({js(tag)}).toString()", [tag],
            "L9 L8", E("sec-intl-locale-constructor", "sec-canonicalizeunicodelocaleid"),
            "rows L9 and L8, through Intl.Locale")
    # L10: a -t- tlang whose language alias adds a subtag.
    for tag in ("en-t-sh", "de-t-mo", "en-t-sgn-gr"):
        add("a", "aliases", gcl, f"{gcl}({js(tag)})[0]", [tag], "L10", E("sec-canonicalizeunicodelocaleid"),
            "row L10: the -t- extension's tlang is canonicalized as a language tag, an alias's added subtags "
            "included")
    # L3 (same) and L12: likely subtags.
    for tag, method in (("zh-TW", "maximize"), ("und", "maximize"), ("und-Arab", "maximize"), ("sr-ME", "maximize"),
                        ("zh-Hant-TW", "minimize"), ("en-Latn-US", "minimize"), ("und-Zzzz", "maximize"),
                        ("und-ZZ", "maximize"), ("und-Zzzz-ZZ", "maximize"), ("und-Zzzz-ZZ", "minimize"),
                        ("en-Zzzz-US", "minimize")):
        row = "L12" if "Zzzz" in tag or "ZZ" in tag else "L3"
        add("a", "likely", f"Intl.Locale.prototype.{method}", f"new Intl.Locale({js(tag)}).{method}().toString()",
            [tag], row, E(f"sec-Intl.Locale.prototype.{method}"),
            "row L12: Zzzz and ZZ are unknown values, which Add Likely Subtags replaces" if row == "L12"
            else "row L3 (same): Add and Remove Likely Subtags over CLDR's likelySubtags")
    # L14: letters outside ASCII in option values.
    variants = add("a", "options", "Intl.Locale (variants option)",
                   f'new Intl.Locale("en", {{variants: {js(chr(0x212a) + "aaaa")}}}).toString()', ["en", "Kaaaa"],
                   "L14", E("sec-intl-locale-constructor", "sec-updatelanguageid"),
                   "row L14: the variants option must be variant subtags, ASCII, so a KELVIN SIGN is refused",
                   text="error: RangeError",
                   text_reason="Intl.Locale's UpdateLanguageId reads the variants option and throws a RangeError "
                               "unless it is a sequence of unicode_variant_subtag; the reference does not read the "
                               "option at all (V8's --js-intl-locale-variants is off by default)")
    del variants
    for option, value in (("numberingSystem", "Khmr"), ("collation", "phoKebk"),
                          ("firstDayOfWeek", "moK"), ("calendar", "gregorİ"), ("language", "Ko"),
                          ("region", "UK")):
        add("a", "options", f"Intl.Locale ({option} option)", f'new Intl.Locale("en", {{{option}: {js(value)}}}).toString()',
            ["en", value], "L14", E("sec-intl-locale-constructor"),
            "row L14: an option value must be ASCII type subtags; a letter culture casing folds into ASCII is "
            "refused")
    add("a", "options", "Intl.NumberFormat (numberingSystem option)",
        f'new Intl.NumberFormat("en", {{numberingSystem: {js(chr(0x212a) + "hmr")}}}).resolvedOptions().numberingSystem',
        ["en", "Khmr"], "L14", E("sec-intl.numberformat"),
        "row L14: the numberingSystem option must match the type nonterminal, ASCII")
    # L15: an information method on a Locale that carries the keyword.
    keyed = "en-u-ca-japanese-co-emoji-hc-h23-nu-arab-fw-wed"
    for method in ("getCalendars", "getCollations", "getHourCycles", "getNumberingSystems"):
        add("a", "info", f"Intl.Locale.prototype.{method}", j(f"new Intl.Locale({js(keyed)}).{method}()"), [keyed],
            "L15", E(f"sec-Intl.Locale.prototype.{method}"),
            "row L15: a keyword's value is the whole answer of the matching information method")
    add("a", "info", "Intl.Locale.prototype.getCollations", j('new Intl.Locale("de-u-co-phonebk").getCollations()'),
        ["de-u-co-phonebk"], "L15", E("sec-collationsoflocale"), "row L15: the co keyword is the whole answer")
    add("a", "info", "Intl.Locale.prototype.getHourCycles", j('new Intl.Locale("und-u-hc-h11").getHourCycles()'),
        ["und-u-hc-h11"], "L15", E("sec-hourcyclesoflocale"), "row L15: the hc keyword is the whole answer")
    # L16 (same): calendars of a region whose preference is the Gregorian calendar.
    add("a", "info", "Intl.Locale.prototype.getCalendars", j('new Intl.Locale("en-US").getCalendars()'), ["en-US"],
        "L16", E("sec-calendarsoflocale"), "row L16 (same): the United States' calendar preference")
    # L17 and L18: collations with no keyword (decisions D32 and D33 (c)).
    for tag, vm in (("en", []), ("en-US", []), ("en-GB", []), ("de", ["phonebk"]), ("de-DE", ["phonebk"])):
        add("a", "collations", "Intl.Locale.prototype.getCollations", j(f"new Intl.Locale({js(tag)}).getCollations()"),
            [tag], "L17", E("sec-collationsoflocale"),
            "row L17: CollationsOfLocale lists the collations of Intl.Collator's data for the locale (decision D33 "
            "(c): each engine's Collator data, which stays its own until SJL-17)",
            per_engine={"broiler-vm": json_list(vm), "broiler-js": json_list(broiler_js_collations(tag))},
            impl_reason="Broiler.VM's Collator data offers phonebk for German alone (JSD-0043), so its plan answers "
                        "that list; Broiler.JS's adapter hands the operation the collations its own Collator, which "
                        "keeps its host path until SJL-17, resolves for the locale (decision D33 (c))")
    for tag in ("fr-FR", "es-419", "ja", "zh-TW", "ar-EG"):
        add("a", "collations", "Intl.Locale.prototype.getCollations", j(f"new Intl.Locale({js(tag)}).getCollations()"),
            [tag], "L18 L17", E("sec-collationsoflocale"),
            "rows L18 (a correction of Broiler.VM, decision D32) and L17: for a language the Collator does not "
            "support, CollationsOfLocale reads DefaultLocale's collations, and otherwise the locale's",
            per_engine={"broiler-vm": json_list([]), "broiler-js": json_list(broiler_js_collations(tag))},
            impl_reason="Broiler.VM's DefaultLocale is en-US, whose Collator data holds no collation (decision D32); "
                        "Broiler.JS's adapter hands the operation the collations its own Collator resolves for the "
                        "locale (decision D33 (c)), emoji and eor and the language's tailorings")
    add("a", "collations", "Intl.Locale.prototype.getCollations", j('new Intl.Locale("und").getCollations()'), ["und"],
        "L18 L17", E("sec-collationsoflocale"),
        "rows L18 and L17: und answers emoji and eor, CollationsOfLocale's own list")
    # L19, L20, L21: hour cycles and week data by region (text: CLDR's time data; see hour_cycle_cases).
    # L22: numbering systems of a locale with no keyword.
    for tag in ("ar-EG", "ar-SA", "fa-IR", "ckb-IQ", "en-US", "de"):
        add("a", "info", "Intl.Locale.prototype.getNumberingSystems",
            j(f"new Intl.Locale({js(tag)}).getNumberingSystems()"), [tag], "L22", E("sec-numberingsystemsoflocale"),
            "row L22 (data, C5): the locale's default numbering system, CLDR 48's",
            per_engine_ref={"broiler-js": j(f"new Intl.Locale({js(tag)}).getNumberingSystems()"),
                            "broiler-vm": j('["latn"]') if tag.split("-")[0] not in ("de", "en") else
                            j(f"new Intl.Locale({js(tag)}).getNumberingSystems()")}, collapse_same=True,
            impl_reason="decision D5: Broiler.VM's NumberFormat holds de and en only, and NumberingSystemsOfLocale "
                        "answers latn for a locale its NumberFormat does not support; Broiler.JS's embedding carries "
                        "the locale's default system")
    # L24, L25: text direction.
    for tag in ("und-Avst", "und-Chrs", "und-Cprt", "und-Elym", "und-Hatr", "und-Hung", "und-Khar", "und-Lydi",
                "und-Ougr", "ks", "lrc", "bal", "bqi", "pa-PK", "uz-AF", "az-IR", "ku", "sd-IN", "und-IL", "und-SA",
                "he", "en"):
        add("a", "direction", "Intl.Locale.prototype.getTextInfo", f"new Intl.Locale({js(tag)}).getTextInfo().direction",
            [tag], "L24", E("sec-textdirectionoflocale"),
            "row L24 (data, C5): the script, or the likely script, read in CLDR's script metadata")
    for tag, why in (("und-Zzzz", "Zzzz, the unknown script"), ("und-Qaaa", "Qaaa, a private-use script"),
                     ("und-Zyyy", "Zyyy, the common script, whose ordering is neither")):
        add("a", "direction", "Intl.Locale.prototype.getTextInfo", f"new Intl.Locale({js(tag)}).getTextInfo().direction",
            [tag], "L25", E("sec-textdirectionoflocale"),
            f"row L25: the locale's script is {why}; TextDirectionOfLocale returns undefined when no direction is "
            "known", text="value: undefined",
            text_reason="TextDirectionOfLocale returns undefined when the script's ordering is neither right-to-left "
                        "nor left-to-right or cannot be determined; ICU answers ltr by default")
    # L26: week data.
    for tag in ("ar-EG", "en-IN", "fa-IR", "en-MV", "en-US", "de-DE", "en-GB", "he-IL", "pt-BR", "zh-CN"):
        add("a", "week", "Intl.Locale.prototype.getWeekInfo",
            f"(function (w) {{ return JSON.stringify([w.firstDay, w.weekend]); }})(new Intl.Locale({js(tag)}).getWeekInfo())",
            [tag], "L26", E("sec-weekinfooflocale"), "row L26 (data, C5): CLDR's week data for the region")
    # L21: RegionPreference in getWeekInfo (getHourCycles is in hour_cycle_cases).
    for tag in ("en", "ja", "ar", "und", "en-US-u-rg-gbzzzz", "en-u-rg-sazzzz", "zh-u-rg-twzzzz", "en-u-sd-usca",
                "en-u-sd-gbsct", "en-u-fw-xyz", "en-u-fw-mon"):
        add("a", "week", "Intl.Locale.prototype.getWeekInfo",
            f"(function (w) {{ return JSON.stringify([w.firstDay, w.weekend]); }})(new Intl.Locale({js(tag)}).getWeekInfo())",
            [tag], "L21", E("sec-weekinfooflocale", "sec-regionpreference"),
            "row L21: RegionPreference reads the -u-rg- override, the -u-sd- subdivision's region and the likely "
            "region, and a -u-fw- keyword that names a weekday overrides the first day")
    # L28 and L33: the available-locale and default-locale providers (decision D7).
    for tag in ("fr", "ja-JP", "zh-Hant-TW", "sr", "en-GB", "de-AT"):
        vm = {"fr": "en-US", "ja-JP": "en-US", "zh-Hant-TW": "en-US", "sr": "en-US", "en-GB": "en", "de-AT": "de"}[tag]
        add("a", "providers", "Intl.NumberFormat, resolvedOptions().locale",
            f"new Intl.NumberFormat({js(tag)}).resolvedOptions().locale", [tag], "L28",
            E("sec-resolvelocale", "sec-lookupmatchinglocalebybestfit"),
            "row L28 (engine policy, decision D7): the available-locale provider",
            per_engine={"broiler-vm": f"value: str:{quote(vm)}"},
            impl_reason="Broiler.VM's available locales are its data's, de, de-DE, en and en-US, and an unknown "
                        "locale resolves to its default en-US (decision D7); Broiler.JS's are the host's cultures, "
                        "compared with the reference")
    requested = ["ja", "zz", "sw-KE", "de-CH", "en-US-u-ca-gregory", "de-DE-x-foo", "sr", "en-GB", "de-1996"]
    add("a", "providers", "Intl.NumberFormat.supportedLocalesOf",
        f"Intl.NumberFormat.supportedLocalesOf([{', '.join(js(t) for t in requested)}]).join()", requested, "L28",
        E("sec-filterlocales", "sec-lookupmatchinglocalebybestfit"),
        "row L28 (engine policy, decision D7): the available-locale provider",
        per_engine={"broiler-vm": 'value: str:"de-CH,en-US-u-ca-gregory,de-DE-x-foo,en-GB,de-1996"'},
        impl_reason="Broiler.VM's available locales are de, de-DE, en and en-US, so a request is supported when one "
                    "of them is its prefix (decision D7); Broiler.JS's are the host's, compared with the reference")
    for service, script in (("Intl.NumberFormat", "new Intl.NumberFormat().resolvedOptions().locale"),
                            ("Intl.DateTimeFormat", "new Intl.DateTimeFormat().resolvedOptions().locale"),
                            ("Intl.PluralRules", 'new Intl.PluralRules("xx").resolvedOptions().locale'),
                            ("Intl.ListFormat", "new Intl.ListFormat([]).resolvedOptions().locale"),
                            ("Number.prototype.toLocaleString", "(1234.5).toLocaleString()")):
        expectation = 'value: str:"1,234.5"' if "toLocaleString" in script else 'value: str:"en-US"'
        add("a", "providers", service, script, [], "L33", E("sec-defaultlocale", "sec-resolvelocale"),
            "row L33 (engine policy, decision D7): DefaultLocale, read when no locale is requested or none is "
            "available", per_engine={"broiler-vm": expectation, "broiler-js": expectation}, impl_reason=ENGINE_POLICY_D7)
    add("a", "providers", "String.prototype.toLocaleLowerCase", '"I".toLocaleLowerCase()', ["I"], "L35",
        E("sec-transform-case", "sec-defaultlocale"),
        "row L35 (engine policy, decision D7): TransformCase with no locale reads DefaultLocale",
        per_engine={"broiler-vm": 'value: str:"i"', "broiler-js": 'value: str:"i"'}, impl_reason=ENGINE_POLICY_D7)
    add("a", "providers", "Intl.NumberFormat.supportedLocalesOf",
        'Intl.NumberFormat.supportedLocalesOf([new Intl.NumberFormat().resolvedOptions().locale, "en"]).join()', ["en"],
        "L44", E("sec-internal-slots", "sec-defaultlocale"),
        "row L44: [[AvailableLocales]] includes DefaultLocale and its less narrow tags (sec-internal-slots); with no "
        "host culture both engines answer this, and part D of Broiler.JS's rule holds the case under invariant "
        "globalization")
    # L29: the resolved locale is an available locale, not the request.
    for tag in ("en-US-x-foo", "de-DE-1996", "en-US-u-xx-yyy"):
        for service in ("NumberFormat", "DateTimeFormat", "PluralRules", "Collator"):
            prefix = tag.split("-x-")[0].split("-u-")[0].replace("-1996", "")
            add("a", "negotiation", f"Intl.{service}, resolvedOptions().locale",
                f"new Intl.{service}({js(tag)}).resolvedOptions().locale", [tag], "L29", E("sec-resolvelocale"),
                "row L29: ResolveLocale answers the available locale that matched, with the supported extension "
                "keywords, never the request's own variants or private use",
                per_engine={e: f"value: str:{quote(prefix)}" for e in ("broiler-js", "broiler-vm")}, collapse_same=True,
                impl_reason=f"[[AvailableLocales]] is implementation-defined (row L28): both engines' lists hold "
                            f"{prefix} for every service (decision D5), so each answers the prefix that matched, "
                            f"{prefix}; the reference's {service} list holds only the language")
    # L30: the relevant extension keys of each service.
    tag = "en-u-attr-ca-japanese-nu-arab-co-phonebk-kn-kf-upper-hc-h23-xx-yyy"
    for service, ctor in (("ListFormat", f"new Intl.ListFormat({js(tag)})"),
                          ("DisplayNames", f'new Intl.DisplayNames({js(tag)}, {{type: "region"}})'),
                          ("PluralRules", f"new Intl.PluralRules({js(tag)})"),
                          ("Segmenter", f"new Intl.Segmenter({js(tag)})"),
                          ("DurationFormat", f"new Intl.DurationFormat({js(tag)})"),
                          ("RelativeTimeFormat", f"new Intl.RelativeTimeFormat({js(tag)})")):
        add("a", "negotiation", f"Intl.{service}, resolvedOptions().locale", f"{ctor}.resolvedOptions().locale", [tag],
            "L30", E("sec-resolvelocale", f"sec-Intl.{service}" if service not in ("PluralRules", "Segmenter")
                     else f"sec-intl.{service.lower()}"),
            "row L30: ResolveLocale keeps only the service's relevant extension keys")
    add("a", "negotiation", "Intl.PluralRules, resolvedOptions().locale",
        'new Intl.PluralRules("en-u-nu-arab").resolvedOptions().locale', ["en-u-nu-arab"], "L30",
        E("sec-resolvelocale", "sec-intl.pluralrules"), "row L30: PluralRules has no relevant extension key")
    # L32: a Collator option the data lacks (collation data is SJL-17's).
    add("a", "negotiation", "Intl.Collator, resolvedOptions()",
        j('(function (o) { return [o.locale, o.collation]; })(new Intl.Collator("de-u-co-phonebk", {collation: "eor"}).resolvedOptions())'),
        ["de-u-co-phonebk", "eor"], "L32", E("sec-intl.collator", "sec-resolvelocale"),
        "row L32 (data, C5): each engine's collation data, SJL-17's",
        per_engine_ref={"broiler-js": j('(function (o) { return [o.locale, o.collation]; })(new Intl.Collator("de-u-co-phonebk", {collation: "eor"}).resolvedOptions())'),
                        "broiler-vm": j('["de-u-co-phonebk", "phonebk"]')},
        impl_reason="row L32: each engine's Collator data is SJL-17's and no answer moves at SJL-9; Broiler.VM's holds "
                    "phonebk alone for de (JSD-0043), so the option is not supported and the keyword stays; "
                    "Broiler.JS's Collator is the host's until SJL-17")
    # L37: Intl.DisplayNames.supportedLocalesOf.
    add("a", "negotiation", "Intl.DisplayNames.supportedLocalesOf", "typeof Intl.DisplayNames.supportedLocalesOf", [],
        "L37", E("sec-Intl.DisplayNames.supportedLocalesOf"), "row L37: the function exists")
    add("a", "negotiation", "Intl.DisplayNames.supportedLocalesOf",
        'Intl.DisplayNames.supportedLocalesOf(["en-US", "de"]).join()', ["en-US", "de"], "L37",
        E("sec-Intl.DisplayNames.supportedLocalesOf", "sec-filterlocales"), "row L37: it filters as every service's does")
    # L40: the collations supportedValuesOf lists (each engine's Collator; engine policy).
    add("a", "values", "Intl.supportedValuesOf", j('Intl.supportedValuesOf("collation")'), ["collation"], "L40",
        E("sec-intl.supportedvaluesof", "sec-availablecanonicalcollations"),
        "row L40 (engine policy): each engine's Collator's collations, SJL-17's",
        per_engine={"broiler-vm": json_list(["phonebk"]), "broiler-js": json_list(broiler_js_collations())},
        impl_reason="Broiler.VM's Collator offers phonebk alone (JSD-0043); Broiler.JS's Collator, which keeps its "
                    "host path until SJL-17, lists the nine collations it resolves for some locale")
    # L43: CanonicalizeLocaleList over many elements (the charge is not observable here).
    add("a", "values", "Intl.getCanonicalLocales", 'Intl.getCanonicalLocales(Array(2000).fill("en-us")).join()', ["en-us"],
        "L43", E("sec-canonicalizelocalelist"),
        "row L43 (engine policy, decision D18): the path Broiler.VM charges one unit per element; the vector reaches "
        "the path, and the charge is held by Broiler.VM's rule, not here")
    # L23 and L41 (same): time zones of a region, and the canonical calendars. The time-zones area's vectors name the
    # op Intl.Locale.prototype.getTimeZones for the zones joined, and a library-level runner answers a vector by its op
    # and inputs alone, so these two, which render the JSON text and the type, name ops of their own.
    add("a", "values", "Intl.Locale.prototype.getTimeZones, JSON", j('new Intl.Locale("en-NZ").getTimeZones()'),
        ["en-NZ"], "L23", E("sec-timezonesoflocale"),
        "row L23 (same): tzdb's zone.tab zones of the region, in code-unit order")
    add("a", "values", "Intl.Locale.prototype.getTimeZones, typeof", 'typeof new Intl.Locale("en").getTimeZones()',
        ["en"], "L23", E("sec-timezonesoflocale"), "row L23 (same): undefined without a region")


# ---- (b) The Intl mathematical value, digit options and FormatNumericToString; plural rules and ranges ------------

NF = "Intl.NumberFormat.prototype.format"


def nf(locale: str, options: str, value: str) -> str:
    return f"new Intl.NumberFormat({js(locale)}, {{{options}}}).format({value})" if options else \
        f"new Intl.NumberFormat({js(locale)}).format({value})"


def cases_b() -> None:
    # M1a: the mathematical value of a Number is Number::toString's decimal, not the binary value.
    for opts, values in (("maximumFractionDigits: 1", (1.15, 1.25, 1.35, 1.45, -1.15)),
                         ("maximumFractionDigits: 2", (1.005, 2.675, 1.255, 8.345, 0.615)),
                         ('maximumFractionDigits: 2, roundingMode: "halfEven"', (1.015, 1.025, 0.125)),
                         ("minimumSignificantDigits: 4", (1.005,)),
                         ("maximumSignificantDigits: 21", (0.1 + 0.2, 5e-324, 1.7976931348623157e308)),
                         ("maximumFractionDigits: 20", (0.1,)),
                         ("maximumSignificantDigits: 3", (9.995,))):
        for value in values:
            add("b", "math-value", NF, nf("en", opts, num(value)), ["en", value], "M1a",
                E("sec-tointlmathematicalvalue", "sec-formatnumerictostring"),
                f"row M1a: ToIntlMathematicalValue reads Number::toString's digits; {{{opts}}}")
    for script, value in (('new Intl.NumberFormat("en", {style: "percent", maximumFractionDigits: 1}).format(%s)', 1.005),
                          ('new Intl.NumberFormat("en", {style: "currency", currency: "USD"}).format(%s)', 1.005),
                          ('(%s).toLocaleString("en", {maximumFractionDigits: 2})', 1.005),
                          ('JSON.stringify(new Intl.NumberFormat("en", {maximumFractionDigits: 2}).formatToParts(%s))', 2.675)):
        add("b", "math-value", NF, script % num(value), ["en", value], "M1a",
            E("sec-tointlmathematicalvalue", "sec-formatnumerictostring"), "row M1a, through another path")
    # M1b: an object whose ToPrimitive is a String.
    for script in ('new Intl.NumberFormat("en", {maximumFractionDigits: 1}).format({valueOf: function () { return "1.15"; }})',
                   'new Intl.NumberFormat("en", {maximumFractionDigits: 1}).format({toString: function () { return "1.15"; }, valueOf: undefined})',
                   'new Intl.NumberFormat("en").format({valueOf: function () { return "9007199254740993"; }})',
                   '(function () { var n = 0; new Intl.NumberFormat("en").format({valueOf: function () { n++; return "1"; }}); return n; })()'):
        add("b", "math-value", NF, script, ["en"], "M1b", E("sec-tointlmathematicalvalue"),
            "row M1b: ToPrimitive first, once, then the String's exact value")
    # M2a (same) and M2b: a String's exact value, and the zero edge.
    M2B_REASON = ("ToIntlMathematicalValue returns 0, or negative-zero for a negative value, when RoundMVResult of the "
                  "String's value is +0 as a Number, so the formatter writes 0 whatever the digit options")
    for opts, value in (("", '"9007199254740993"'), ("", '"123456789012345678901234567890.5"'),
                        ("maximumFractionDigits: 3", '"0.1234999999999999999999"'), ("", '"0x1F"'),
                        ("", '" 1e3 "'), ("", '"-0"')):
        add("b", "math-value", NF, nf("en", opts, value), ["en", value.strip('"')], "M2a",
            E("sec-tointlmathematicalvalue", "sec-runtime-semantics-stringintlmv"),
            "row M2a (same): StringIntlMV keeps a String's exact decimal value")
    for opts, value in (("maximumSignificantDigits: 2", '"2.4703282292062327e-324"'),
                        ("maximumSignificantDigits: 2", '"1e-400"'), ("maximumSignificantDigits: 2", '"-1e-400"'),
                        ('roundingMode: "ceil", maximumFractionDigits: 1', '"1e-400"'),
                        ("", '"1e400"'), ("", '"-1e400"')):
        zero = {'"2.4703282292062327e-324"': '"0"', '"1e-400"': '"0"', '"-1e-400"': '"-0"'}.get(value)
        add("b", "math-value", NF, nf("en", opts, value), ["en", value.strip('"')], "M2b",
            E("sec-tointlmathematicalvalue"),
            "row M2b: a String whose value rounds to zero, or past the Number range, as a Number: ToIntlMathematicalValue "
            "answers 0, negative-zero or an infinity",
            **(dict(text=f"value: str:{zero}", text_reason=M2B_REASON) if zero else {}))
    # M3: a BigInt.
    for opts, value, label in (("", "123456789012345678901234n", Big(123456789012345678901234)),
                               ('notation: "scientific", maximumSignificantDigits: 21', "123456789012345678901n",
                                Big(123456789012345678901)),
                               ('notation: "engineering", maximumSignificantDigits: 21', "123456789012345678901n",
                                Big(123456789012345678901)),
                               ("", "10n ** 309n", Big(10**309)), ("", "-(10n ** 400n)", Big(-10**400)),
                               ('style: "percent"', "1n", Big(1))):
        add("b", "math-value", NF, nf("en", opts, value), ["en", label], "M3", E("sec-tointlmathematicalvalue"),
            "row M3: a BigInt's exact value in every notation, beyond the Number range too")
    add("b", "math-value", NF, 'new Intl.NumberFormat("en").format(Object(5n))', ["en", Big(5)], "M3",
        E("sec-tointlmathematicalvalue"), "row M3: a BigInt wrapper, through ToPrimitive")
    add("b", "math-value", NF, '(123456789012345678901n).toLocaleString("en", {notation: "scientific", maximumSignificantDigits: 21})',
        ["en", Big(123456789012345678901)], "M3", E("sec-tointlmathematicalvalue"), "row M3, through toLocaleString")
    add("b", "math-value", "Intl.NumberFormat.prototype.formatToParts",
        j('new Intl.NumberFormat("en").formatToParts(10n ** 30n)'), ["en", Big(10**30)], "M3",
        E("sec-tointlmathematicalvalue", "sec-partitionnumberpattern"), "row M3, as parts")
    # M4: SetNumberFormatDigitOptions' reads, coercions and refusals.
    order = ('(function () { var log = []; var o = new Proxy({}, {get: function (t, k) { if (typeof k === "string") '
             'log.push(k); return undefined; }}); new Intl.NumberFormat("en", o); return log.join(); })()')
    add("b", "digit-options", "Intl.NumberFormat (option reads)", order, ["en"], "M4 N13",
        E("sec-intl.numberformat", "sec-setnumberformatdigitoptions"),
        "rows M4 and N13: every option read once, in the specification's order")
    once = ('(function () { var n = 0; var v = {valueOf: function () { n++; return 2; }}; '
            'var f = new Intl.NumberFormat("en", {maximumFractionDigits: v, minimumSignificantDigits: v}); '
            'f.format(1); f.resolvedOptions(); return n; })()')
    add("b", "digit-options", "Intl.NumberFormat (option coercions)", once, ["en"], "M4",
        E("sec-setnumberformatdigitoptions", "sec-defaultnumberoption"),
        "row M4: each option converted once, at construction, and mxfd never when significant digits win")
    add("b", "digit-options", "Intl.NumberFormat (resolvedOptions)",
        'typeof new Intl.NumberFormat("en", {minimumSignificantDigits: {valueOf: function () { return 2; }}}).resolvedOptions().minimumSignificantDigits',
        ["en"], "M4", E("sec-intl.numberformat.prototype.resolvedoptions"), "row M4: the resolved digit option is a Number")
    for opts in ("minimumFractionDigits: 3, maximumFractionDigits: 2", "minimumSignificantDigits: 5, maximumSignificantDigits: 4",
                 "maximumFractionDigits: 101", "minimumIntegerDigits: 0", "maximumSignificantDigits: 22",
                 "roundingIncrement: 3", "roundingIncrement: 5, maximumSignificantDigits: 2",
                 'roundingIncrement: 5, roundingPriority: "morePrecision"',
                 "roundingIncrement: 5, minimumFractionDigits: 2, maximumFractionDigits: 3",
                 'roundingPriority: "most"', 'trailingZeroDisplay: "strip"', 'roundingMode: "up"'):
        add("b", "digit-options", "Intl.NumberFormat (refusals)", f'new Intl.NumberFormat("en", {{{opts}}}).format(1)', ["en"],
            "M4", E("sec-setnumberformatdigitoptions", "sec-defaultnumberoption", "sec-getoption"),
            f"row M4: {{{opts}}} is refused")
    # M5: the defaults SetNumberFormatDigitOptions is handed, and resolvedOptions' digit properties.
    # M5: an increment with fraction digits. SetNumberFormatDigitOptions sets mxfdDefault to mnfdDefault when the
    # increment is not 1, so {roundingIncrement: 25, minimumFractionDigits: 2} resolves 2/2 and {roundingIncrement: 5}
    # resolves 0/0, and neither is refused; the reference refuses both with a RangeError.
    M5_REASON = ("SetNumberFormatDigitOptions sets mxfdDefault to mnfdDefault when roundingIncrement is not 1, so the "
                 "maximum resolves to the minimum and the increment's check passes; the value rounds to the increment")
    M5_RESOLVED = {"roundingIncrement: 25, minimumFractionDigits: 2": [1, 2, 2, None, None, 25, "halfExpand", "auto", "auto"],
                   "roundingIncrement: 5": [1, 0, 0, None, None, 5, "halfExpand", "auto", "auto"]}
    keys = ("minimumIntegerDigits", "minimumFractionDigits", "maximumFractionDigits", "minimumSignificantDigits",
            "maximumSignificantDigits", "roundingIncrement", "roundingMode", "roundingPriority", "trailingZeroDisplay")
    pick = "(function (o) { return JSON.stringify([" + ", ".join(f"o.{k}" for k in keys) + "]); })"
    for opts in ("", "maximumFractionDigits: 1", "minimumFractionDigits: 5", 'style: "percent"',
                 'style: "percent", minimumFractionDigits: 1', 'style: "currency", currency: "JPY"',
                 'style: "currency", currency: "KWD"', 'style: "currency", currency: "EUR", maximumFractionDigits: 0',
                 'notation: "compact"', 'style: "currency", currency: "USD", notation: "compact"',
                 'notation: "compact", maximumFractionDigits: 1', 'style: "currency", currency: "USD", notation: "scientific"',
                 "maximumSignificantDigits: 3", 'roundingPriority: "morePrecision"', 'roundingPriority: "lessPrecision"',
                 "roundingIncrement: 25, minimumFractionDigits: 2", "roundingIncrement: 5"):
        add("b", "digit-options", "Intl.NumberFormat.prototype.resolvedOptions",
            f'{pick}(new Intl.NumberFormat("en", {{{opts}}}).resolvedOptions())', ["en"], "M5",
            E("sec-intl.numberformat", "sec-setnumberformatdigitoptions", "sec-intl.numberformat.prototype.resolvedoptions"),
            f"row M5: the digit options {{{opts}}} resolve to",
            **({opts_: dict(text=f"value: str:{quote(json.dumps(picked, separators=(',', ':')))}", text_reason=M5_REASON)
                for opts_, picked in M5_RESOLVED.items()}.get(opts, {})))
    for opts, value in (('style: "percent"', 0.125), ('style: "percent"', 0.505), ('style: "percent"', 0.0049),
                        ("roundingIncrement: 25, minimumFractionDigits: 2", 1.13), ("roundingIncrement: 5", 7)):
        add("b", "digit-options", NF, nf("en", opts, num(value)), ["en", value], "M5",
            E("sec-setnumberformatdigitoptions", "sec-formatnumerictostring"), f"row M5: {{{opts}}} formats",
            **({"roundingIncrement: 25, minimumFractionDigits: 2": dict(text='value: str:"1.25"', text_reason=M5_REASON),
                "roundingIncrement: 5": dict(text='value: str:"5"', text_reason=M5_REASON)}.get(opts, {})))
    # M8 (same): negative zero. M6 (same): the rounding modes, where 1.35, whose double is not 1.35, also reaches
    # M1a (ToIntlMathematicalValue reads a Number by its shortest digits, so 1.35 is a tie).
    for opts, value in (("", -0.0), ('signDisplay: "negative"', -0.0), ('signDisplay: "exceptZero"', -0.0),
                        ('roundingMode: "halfExpand", maximumFractionDigits: 0', -0.4)):
        add("b", "math-value", NF, nf("en", opts, num(value)), ["en", value], "M8",
            E("sec-partitionnumberpattern", "sec-formatnumerictostring"), "row M8 (same): negative zero and its sign")
    for mode in ("ceil", "floor", "expand", "trunc", "halfCeil", "halfFloor", "halfExpand", "halfTrunc", "halfEven"):
        for value in (1.25, -1.25, 1.35):
            add("b", "math-value", NF, nf("en", f'maximumFractionDigits: 1, roundingMode: "{mode}"', num(value)),
                ["en", value, mode], "M6 M1a" if value == 1.35 else "M6",
                E("sec-formatnumerictostring", "sec-applyunsignedroundingmode")
                + (E("sec-tointlmathematicalvalue") if value == 1.35 else ()),
                f"row M6 (same): the rounding mode {mode}"
                + ("; and row M1a: the value is Number::toString's digits, 1.35, a tie" if value == 1.35 else ""))
    # M7: trailingZeroDisplay.
    for opts, value in (("minimumFractionDigits: 2", 5), ("minimumFractionDigits: 2", 5.1), ("minimumFractionDigits: 2", 4.9999),
                        ("minimumFractionDigits: 2", 5.001), ("minimumSignificantDigits: 2, maximumSignificantDigits: 2", 1),
                        ('style: "currency", currency: "EUR"', 5), ('style: "percent", minimumFractionDigits: 1', 0.05)):
        add("b", "digit-options", NF, nf("en", opts + ', trailingZeroDisplay: "stripIfInteger"', num(value)), ["en", value],
            "M7", E("sec-formatnumerictostring"), "row M7: stripIfInteger cuts an integral value at its point")
    add("b", "digit-options", "Intl.NumberFormat.prototype.formatToParts",
        j('new Intl.NumberFormat("en", {minimumFractionDigits: 2, trailingZeroDisplay: "stripIfInteger"}).formatToParts(5)'),
        ["en", 5], "M7", E("sec-formatnumerictostring"), "row M7, as parts")
    # M9: a very long String (the charge is not observable here).
    add("b", "math-value", NF,
        'new Intl.NumberFormat("en", {maximumFractionDigits: 100}).format("0." + "1".repeat(100000)).length',
        ["en"], "M9", E("sec-tointlmathematicalvalue", "sec-formatnumerictostring"),
        "row M9 (engine policy, decision D18): the path Broiler.VM charges by its digits; the vector reaches it, and "
        "Broiler.VM's rule holds the charge")
    # P1: select over FormatNumericToString's string.
    for locale, opts, value, ptype in (("en", "minimumFractionDigits: 1", 1, ""), ("de", "minimumFractionDigits: 1", 1, ""),
                                       ("en", "maximumFractionDigits: 0", 1.4, ""), ("en", "maximumFractionDigits: 0", 0.6, ""),
                                       ("en", "", 1.0001, ""), ("en", "", 0.9999, ""), ("en", "", 1.0005, ""),
                                       ("en", "maximumSignificantDigits: 1", 1.5, ""), ("en", "minimumSignificantDigits: 2", 1, ""),
                                       ("en", 'maximumFractionDigits: 0, roundingMode: "floor"', 1.9, ""),
                                       ("en", 'maximumFractionDigits: 0, roundingMode: "ceil"', 0.1, ""),
                                       ("en", "maximumFractionDigits: 0", 1.4, "ordinal"),
                                       ("en", 'notation: "compact"', 0.999, ""), ("en", 'notation: "compact"', 1.004, "")):
        o = ", ".join(x for x in (opts, f'type: "{ptype}"' if ptype else "") if x)
        add("b", "plurals", "Intl.PluralRules.prototype.select",
            f"new Intl.PluralRules({js(locale)}, {{{o}}}).select({num(value)})", [locale, value], "P1",
            E("sec-resolveplural", "sec-formatnumerictostring", "sec-pluralruleselect"),
            "row P1: ResolvePlural selects over FormatNumericToString's string, with the digit options")
    # P2: select and selectRange of a BigInt. The pinned text reads ToIntlMathematicalValue, which takes a BigInt as
    # its mathematical value; the reference's V8 still reads ToNumber and throws a TypeError, so each answer is the
    # text's: the category of the same value as a Number under CLDR's rules, which the reference gives for the Number.
    p2_reason = ("the pinned text's select and selectRange read ToIntlMathematicalValue, which takes a BigInt as its "
                 "mathematical value, and ResolvePlural selects over its formatted digits; the category is the one the "
                 "reference gives for the same value as a Number")
    for locale, value, ptype, answer in (("en", "1n", "", "one"), ("en", "2n", "", "other"), ("en", "0n", "", "other"),
                                         ("en", "-1n", "", "one"), ("en", "2n", "ordinal", "two"),
                                         ("en", "3n", "ordinal", "few"), ("en", "10n ** 30n", "", "other"),
                                         ("en", "10n ** 400n", "", "other"), ("en", "Object(1n)", "", "one")):
        o = f'{{type: "{ptype}"}}' if ptype else "{}"
        add("b", "plurals", "Intl.PluralRules.prototype.select", f"new Intl.PluralRules({js(locale)}, {o}).select({value})",
            [locale], "P2", E("sec-intl.pluralrules.prototype.select", "sec-tointlmathematicalvalue"),
            "row P2: select reads ToIntlMathematicalValue, so a BigInt is a value",
            text=f"value: str:{quote(answer)}", text_reason=p2_reason)
    add("b", "plurals", "Intl.PluralRules.prototype.select", 'new Intl.PluralRules("en", {minimumFractionDigits: 1}).select(1n)',
        ["en"], "P2", E("sec-intl.pluralrules.prototype.select"), "row P2 with digit options: 1n is written 1.0",
        text='value: str:"other"', text_reason=p2_reason)
    add("b", "plurals", "Intl.PluralRules.prototype.selectRange", 'new Intl.PluralRules("de").selectRange(0n, 1n)', ["de"],
        "P2", E("sec-intl.pluralrules.prototype.selectrange"), "row P2: selectRange of BigInts",
        text='value: str:"one"', text_reason=p2_reason)
    # The same values as Numbers, which the reference answers, so the text's answers above are held to it.
    for locale, value, ptype in (("en", "1", ""), ("en", "2", ""), ("en", "0", ""), ("en", "-1", ""),
                                 ("en", "2", "ordinal"), ("en", "3", "ordinal")):
        o = f'{{type: "{ptype}"}}' if ptype else "{}"
        add("b", "plurals", "Intl.PluralRules.prototype.select", f"new Intl.PluralRules({js(locale)}, {o}).select({value})",
            [locale], "P2", E("sec-intl.pluralrules.prototype.select"),
            "row P2's witness: the Number of the same value as a BigInt vector above")
    add("b", "plurals", "Intl.PluralRules.prototype.selectRange", 'new Intl.PluralRules("de").selectRange(0, 1)', ["de"],
        "P2", E("sec-intl.pluralrules.prototype.selectrange"), "row P2's witness: the Numbers of the BigInt range above")
    # P3: which languages' rules each engine holds (data).
    for locale in ("fr", "ru", "pl", "cs", "uk", "hi", "he", "ar", "cy", "ja", "pt", "pt-PT"):
        add("b", "plurals", "Intl.PluralRules.prototype.resolvedOptions",
            j(f"new Intl.PluralRules({js(locale)}).resolvedOptions().pluralCategories"), [locale], "P3",
            E("sec-intl.pluralrules.prototype.resolvedoptions", "sec-pluralruleselect"),
            "row P3 (data, C5): the language's cardinal categories")
    for locale in ("fr", "sv"):
        add("b", "plurals", "Intl.PluralRules.prototype.resolvedOptions",
            j(f'new Intl.PluralRules({js(locale)}, {{type: "ordinal"}}).resolvedOptions().pluralCategories'), [locale],
            "P3", E("sec-intl.pluralrules.prototype.resolvedoptions"), "row P3 (data, C5): the ordinal categories")
    # P5a, P5b, P5c: selectRange.
    P5A_REASON = ("ResolvePluralRange returns the start's category when both ends' formatted strings are equal; "
                  "FormatNumericToString's string carries no sign, so 1 and 1, 1 and 1.0001, and -1 and 1 are each "
                  "\"1\" and the start's category, one")
    for locale, a, b, opts, rows, note in (
            ("en", 1, 1, "", "P5a", "both ends format alike"), ("en", 1, 1.0001, "", "P5a", "both ends format alike"),
            ("en", -1, 1, "", "P5a", "both ends format alike"), ("de", 1, 1, "", "P5a", "both ends format alike"),
            ("en", 1, 1, 'type: "ordinal"', "P5a", "both ends format alike"),
            ("en", 1, 1, "minimumFractionDigits: 1", "P5a", "both ends format alike"),
            ("de", 0, 1, "", "P5b", "CLDR's plural ranges"), ("de", 1, 2, "", "P5b", "CLDR's plural ranges"),
            ("de", 2, 1, "", "P5b", "CLDR's plural ranges"), ("de", 1, 1.5, "", "P5b", "CLDR's plural ranges"),
            ("en", 0, 1, "", "P5b", "CLDR's plural ranges"), ("en", 1, 2, "", "P5b", "CLDR's plural ranges"),
            ("en", 2, 1, "", "P5b", "CLDR's plural ranges"), ("en", 1, 2, 'type: "ordinal"', "P5b", "an ordinal range"),
            ("en", 2, 3, 'type: "ordinal"', "P5b", "an ordinal range"), ("fr", 0, 1, "", "P5b", "CLDR's plural ranges"),
            ("fr", 1, 2, "", "P5b", "CLDR's plural ranges"), ("en", 1, 5, "", "P5c", "a range in order")):
        extra = {}
        if rows == "P5b":
            extra = dict(per_engine_same=True, impl_reason=LIBRARY_TAKES)
        elif rows == "P5a" and opts != "minimumFractionDigits: 1":
            extra = dict(text='value: str:"one"', text_reason=P5A_REASON)
        add("b", "plurals", "Intl.PluralRules.prototype.selectRange",
            f"new Intl.PluralRules({js(locale)}, {{{opts}}}).selectRange({num(a)}, {num(b)})", [locale, a, b], rows,
            E("sec-resolvepluralrange", "sec-pluralruleselectrange"),
            f"row {rows}: {note} (P5b: PluralRuleSelectRange is implementation-defined, and the library adopts "
            "CLDR's plural ranges, an X1 adoption)" if rows == "P5b" else f"row {rows}: {note}", **extra)
    add("b", "plurals", "Intl.PluralRules.prototype.selectRange", 'new Intl.PluralRules("en").selectRange(5, 1)', ["en"],
        "P5c", E("sec-resolvepluralrange"), "row P5c (same): a decreasing range is no error")
    add("b", "plurals", "Intl.PluralRules.prototype.selectRange", 'new Intl.PluralRules("en").selectRange(NaN, 1)', ["en"],
        "P5c", E("sec-resolvepluralrange"), "row P5c (same): NaN is a RangeError")
    # P6: resolvedOptions. SetNumberFormatDigitOptions sets both fraction-digit slots for compact notation (0 and 0)
    # and under a rounding priority (the defaults, 0 and 3), and resolvedOptions writes every slot that is not
    # undefined; the reference leaves the two out.
    def with_fraction_digits(low: int, high: int) -> Callable[[str], str]:
        def text(ref: str) -> str:
            value = json.loads(decode_str(ref))
            if isinstance(value, list):
                if "minimumFractionDigits" in value:
                    return ref
                at = value.index("minimumIntegerDigits") + 1
                value[at:at] = ["minimumFractionDigits", "maximumFractionDigits"]
            else:
                if "minimumFractionDigits" in value:
                    return ref
                items = list(value.items())
                at = [k for k, _ in items].index("minimumIntegerDigits") + 1
                items[at:at] = [("minimumFractionDigits", low), ("maximumFractionDigits", high)]
                value = dict(items)
            return "value: str:" + quote(json.dumps(value, separators=(",", ":"), ensure_ascii=False))
        return text
    p6_text = {'notation: "compact"': with_fraction_digits(0, 0),
               'notation: "compact", compactDisplay: "long"': with_fraction_digits(0, 0),
               'roundingPriority: "morePrecision"': with_fraction_digits(0, 3)}
    P6_REASON = ("SetNumberFormatDigitOptions sets [[MinimumFractionDigits]] and [[MaximumFractionDigits]] for compact "
                 "notation (0 and 0) and under a rounding priority (the defaults, 0 and 3), and resolvedOptions writes "
                 "every slot of its table that is not undefined")
    for opts in ("", 'type: "ordinal"', 'notation: "compact"', 'notation: "compact", compactDisplay: "long"',
                 "maximumSignificantDigits: 3", 'roundingPriority: "morePrecision"',
                 'roundingIncrement: 5, maximumFractionDigits: 2, minimumFractionDigits: 2, roundingMode: "floor", trailingZeroDisplay: "stripIfInteger"'):
        extra = dict(text_fn=p6_text[opts], text_reason=P6_REASON) if opts in p6_text else {}
        add("b", "plurals", "Intl.PluralRules.prototype.resolvedOptions",
            f'JSON.stringify(Object.keys(new Intl.PluralRules("en", {{{opts}}}).resolvedOptions()))', ["en"], "P6",
            E("sec-intl.pluralrules.prototype.resolvedoptions"), "row P6: every property of the table, in its order",
            **extra)
        add("b", "plurals", "Intl.PluralRules.prototype.resolvedOptions",
            f'JSON.stringify(new Intl.PluralRules("en", {{{opts}}}).resolvedOptions())', ["en"], "P6",
            E("sec-intl.pluralrules.prototype.resolvedoptions", "sec-setnumberformatdigitoptions"),
            "row P6: the resolved values", **extra)
    # P7: the PluralRules constructor's reads and refusals.
    order = ('(function () { var log = []; var o = new Proxy({}, {get: function (t, k) { if (typeof k === "string") '
             'log.push(k); return undefined; }}); new Intl.PluralRules("en", o); return log.join(); })()')
    add("b", "plurals", "Intl.PluralRules (option reads)", order, ["en"], "P7", E("sec-intl.pluralrules"),
        "row P7: localeMatcher, type, notation, compactDisplay, then the digit options")
    for opts in ('compactDisplay: "bogus"', "minimumFractionDigits: 3, maximumFractionDigits: 2",
                 "roundingIncrement: 5, maximumSignificantDigits: 2", 'type: "bogus"', 'notation: "bogus"',
                 "roundingIncrement: 3"):
        add("b", "plurals", "Intl.PluralRules (refusals)", f'new Intl.PluralRules("en", {{{opts}}}).select(1)', ["en"],
            "P7", E("sec-intl.pluralrules", "sec-setnumberformatdigitoptions", "sec-getoption"),
            f"row P7: {{{opts}}} is refused")
    # P8 (same): select of special values.
    for value in ("NaN", "Infinity", "-Infinity", "-0"):
        add("b", "plurals", "Intl.PluralRules.prototype.select", f'new Intl.PluralRules("en").select({value})', ["en"],
            "P8", E("sec-resolveplural"), "row P8 (same): a value that is not finite, or negative zero")


# ---- (c) Number formatting; list, relative-time and display-name formatting ----------------------------------------


def cases_c(cldr: Cldr) -> None:
    num_clauses = E("sec-partitionnumberpattern", "sec-getnumberformatpattern")
    # N1a (same) and N1b (data): decimal symbols and grouping.
    for locale in ("en", "en-US", "de", "de-DE"):
        for value in (1234567.891, -0.5, 1e21):
            add("c", "decimal", NF, nf(locale, "", num(value)), [locale, value], "N1a", num_clauses,
                "row N1a (same): the locale's decimal and group separators and grouping")
    for locale in ("de-AT", "de-CH", "fr", "sv", "es", "pl", "en-IN", "ar", "zh-TW"):
        for value in (-1234.5, 10000000, 1234):
            add("c", "decimal", NF, nf(locale, "", num(value)), [locale, value], "N1b", num_clauses,
                "row N1b (data, C5): CLDR's symbols and grouping for the locale")
    add("c", "decimal", NF, 'new Intl.NumberFormat("zh-TW").format(NaN)', ["zh-TW"], "N1b", num_clauses,
        "row N1b (data, C5): NaN's symbol")
    for useg, value in (('useGrouping: "min2"', 1234), ('useGrouping: "min2"', 12345), ('useGrouping: "always"', 1234),
                        ("useGrouping: false", 1234567)):
        add("c", "decimal", NF, nf("en-IN", useg, num(value)), ["en-IN", value], "N1b", num_clauses,
            "row N1b (data, C5): en-IN's grouping under useGrouping")
    # N2a (same), N2b (data), N2c (implementation-defined, decision D35): percent.
    for locale in ("en", "de"):
        add("c", "percent", NF, nf(locale, 'style: "percent"', num(0.5)), [locale, 0.5], "N2a", num_clauses,
            "row N2a (same): the percent pattern")
    for locale in ("de-CH", "fr", "sv", "tr", "es", "ar"):
        for value in (0.5, -0.5):
            add("c", "percent", NF, nf(locale, 'style: "percent"', num(value)), [locale, value], "N2b", num_clauses,
                "row N2b (data, C5): the locale's percent pattern")
    for locale in ("en", "de"):
        for display in ("short", "long", "narrow"):
            add("c", "percent", NF, nf(locale, f'style: "unit", unit: "percent", unitDisplay: "{display}"', "50"),
                [locale, 50, display], "N2c", num_clauses,
                "row N2c (implementation-defined, decision D35): the percent unit in every unitDisplay keeps the "
                "percent pattern",
                per_engine_ref={e: f'new Intl.NumberFormat({js(locale)}, {{style: "percent"}}).format({num(0.5)})'
                                for e in ("broiler-vm", "broiler-js")},
                impl_reason="decision D35: both engines write the percent pattern, which is what style percent writes "
                            "for the same quantity; CLDR's unit pattern, the reference's long answer, departs from it",
            )
    # N3a (same), N3b, N3c (data), N4: currency.
    for locale, currency in (("en", "USD"), ("en", "EUR"), ("de", "EUR"), ("en-US", "JPY")):
        add("c", "currency", NF, nf(locale, f'style: "currency", currency: "{currency}"', num(1234.5)),
            [locale, currency, 1234.5], "N3a", num_clauses, "row N3a (same): the currency pattern and symbol")
    for currency in ("EUR", "USD", "JPY", "GBP", "CHF", "KWD", "BTN", "XYZ"):
        for display in ("code", "symbol", "narrowSymbol"):
            add("c", "currency", NF, nf("en", f'style: "currency", currency: "{currency}", currencyDisplay: "{display}"', "1"),
                ["en", currency, display, 1], "N3b", num_clauses,
                "row N3b (data, C5): CLDR's currencySpacing between a letter and the digits")
    for locale in ("fr", "es", "pt-PT", "ko-KR", "zh-TW", "sv", "ar"):
        for currency in ("EUR", "USD"):
            add("c", "currency", NF, nf(locale, f'style: "currency", currency: "{currency}"', num(-1234.5)),
                [locale, currency, -1234.5], "N3c", num_clauses, "row N3c (data, C5): the locale's currency pattern")
    for locale in ("en", "de"):
        for currency in ("EUR", "USD", "JPY", "XYZ"):
            for value in (0, 1, 2, 1.5, -1234.5):
                add("c", "currency", NF,
                    nf(locale, f'style: "currency", currency: "{currency}", currencyDisplay: "name"', num(value)),
                    [locale, currency, value], "N4", num_clauses,
                    "row N4: currencyDisplay name writes the currency's name, by the value's plural category")
    add("c", "currency", "Intl.NumberFormat.prototype.formatToParts",
        j('new Intl.NumberFormat("en", {style: "currency", currency: "EUR", currencyDisplay: "name"}).formatToParts(1)'),
        ["en", "EUR", 1], "N4", num_clauses, "row N4, as parts")
    # N5a (same), N5b, N5c (implementation-defined), N5d (data): units.
    for locale in ("en", "de"):
        for unit in ("kilometer-per-hour", "liter", "byte"):
            for display in ("short", "long", "narrow"):
                add("c", "unit", NF, nf(locale, f'style: "unit", unit: "{unit}", unitDisplay: "{display}"', "2"),
                    [locale, unit, display, 2], "N5a", num_clauses, "row N5a (same): CLDR's unit pattern")
    for locale in ("en", "de"):
        for unit in ("liter-per-kilometer", "byte-per-second", "kilobit-per-stone"):
            for display in ("short", "long", "narrow"):
                answer = "value: str:" + quote(vm_unit_answer(cldr, locale, unit, display, 2))
                add("c", "unit", NF, nf(locale, f'style: "unit", unit: "{unit}", unitDisplay: "{display}"', "2"),
                    [locale, unit, display, 2], "N5b", num_clauses,
                    "row N5b (implementation-defined): a compound unit with no pattern of its own, composed from its "
                    "two units and CLDR's per pattern",
                    per_engine={"broiler-vm": answer, "broiler-js": answer}, extra_prov=(REF_CLDR,),
                    impl_reason=N5B_REASON)
    add("c", "unit", NF, nf("en", 'style: "unit", unit: "liter", unitDisplay: "long", minimumFractionDigits: 1', "1"),
        ["en", "liter", 1], "N5c", num_clauses,
        "row N5c (implementation-defined): the unit's plural reads the formatted digits", per_engine_same=True,
        impl_reason=LIBRARY_TAKES)
    for locale in ("ja", "ko", "zh-TW", "fr", "es", "pl", "ar"):
        for unit, display in (("kilometer-per-hour", "long"), ("hour", "long"), ("kilometer-per-hour", "short")):
            for value in (1, 2):
                add("c", "unit", NF, nf(locale, f'style: "unit", unit: "{unit}", unitDisplay: "{display}"', num(value)),
                    [locale, unit, display, value], "N5d", num_clauses, "row N5d (data, C5): the locale's unit pattern")
    # N6: compact notation.
    for locale in ("en", "de"):
        for display in ("short", "long"):
            for value in (999.5, 999.995, 999999, 999999.9999999999, 1e21, 1e24):
                add("c", "compact", NF, nf(locale, f'notation: "compact", compactDisplay: "{display}"', num(value)),
                    [locale, display, value], "N6a N6f", E("sec-computeexponent", "sec-getnotationsubpattern"),
                    "rows N6a and N6f: ComputeExponent's exponent after rounding, over the exact value")
    for opts, value in (('notation: "compact", roundingPriority: "morePrecision"', 5e-324),
                        ('notation: "compact", maximumSignificantDigits: 3', 12345),
                        ('notation: "compact", maximumSignificantDigits: 3', 1234567)):
        add("c", "compact", NF, nf("en", opts, num(value)), ["en", value], "N6b",
            E("sec-setnumberformatdigitoptions", "sec-formatnumerictostring"),
            "row N6b: compact rounds by its digit options")
    for locale in ("en", "de"):
        for opts in ('style: "currency", currency: "USD", notation: "compact"',
                     'style: "currency", currency: "USD", notation: "compact", compactDisplay: "long"',
                     'style: "currency", currency: "EUR", currencyDisplay: "name", notation: "compact"',
                     'style: "currency", currency: "USD", notation: "scientific"',
                     'style: "currency", currency: "USD", notation: "engineering"'):
            for value in (1234567, 1234.5):
                add("c", "compact", NF, nf(locale, opts, num(value)), [locale, value], "N6c",
                    E("sec-getnotationsubpattern", "sec-partitionnumberpattern"),
                    "row N6c: the notation applies to every style")
    for locale in ("de",):
        for value in (1e6, 2e6, 1e9):
            add("c", "compact", NF, nf(locale, 'notation: "compact", compactDisplay: "long"', num(value)),
                [locale, value], "N6d", E("sec-getnotationsubpattern"), "row N6d (data, C5): compact patterns and plurals")
    for locale in ("ja", "ko", "zh", "zh-TW", "en-IN", "fr", "es", "pt", "ru"):
        for value in (12345, 1234567, 1000000000):
            add("c", "compact", NF, nf(locale, 'notation: "compact"', num(value)), [locale, value], "N6d",
                E("sec-getnotationsubpattern"), "row N6d (data, C5): the locale's compact patterns")
    add("c", "compact", NF, nf("en", 'notation: "compact"', '"123456789012345678901234567890.5"'),
        ["en", "123456789012345678901234567890.5"], "N6e", E("sec-tointlmathematicalvalue", "sec-computeexponent"),
        "row N6e: the String's exact value in the compact path")
    # N7a (same), N7b (data): scientific and engineering.
    for locale in ("en", "de"):
        for notation in ("scientific", "engineering"):
            add("c", "scientific", NF, nf(locale, f'notation: "{notation}"', num(-1.5e-7)), [locale, -1.5e-7], "N7a",
                E("sec-partitionnotationsubpattern"), "row N7a (same): the exponent symbols")
    for locale in ("sv", "fa", "zh-TW"):
        add("c", "scientific", NF, nf(locale, 'notation: "scientific"', num(-1.5e-7)), [locale, -1.5e-7], "N7b",
            E("sec-partitionnotationsubpattern"), "row N7b (data, C5): the locale's exponent symbols")
    # N8a (same), N8b (data), with decision D21's numbering-system symbols.
    for locale in ("en-u-nu-arab", "de-u-nu-arab", "en-u-nu-thai", "en-u-nu-hanidec"):
        add("c", "numbering", NF, nf(locale, "", num(-1234.5)), [locale, -1234.5], "N8a",
            E("sec-partitionnumberpattern", "sec-numberingsystem-identifiers"),
            "row N8a: the numbering system's digits, and its own CLDR symbols (decision D21: Broiler.VM keeps the latn "
            "symbols, Broiler.JS's embedding carries the system's own)")
    for locale in ("ar", "ar-EG", "ar-u-nu-latn", "fa", "hi-IN-u-nu-deva", "th-u-nu-thai"):
        add("c", "numbering", NF, nf(locale, "", num(-9876.5)), [locale, -9876.5], "N8b",
            E("sec-partitionnumberpattern"), "row N8b (data, C5): the locale's default or native system")
    # N9: ranges.
    for locale in ("en", "de"):
        for opts, a, b in (("", -1, 1), ('signDisplay: "always"', 3, 5), ('style: "percent"', 3, 5),
                           ('style: "currency", currency: "EUR", currencyDisplay: "code"', 3, 5),
                           ('style: "unit", unit: "kilometer"', 3, 5), ('notation: "compact"', 1000, 5000),
                           ('notation: "scientific"', 3, 5),
                           ('style: "currency", currency: "USD", currencySign: "accounting"', -5, -1)):
            add("c", "range", "Intl.NumberFormat.prototype.formatRange",
                f"new Intl.NumberFormat({js(locale)}, {{{opts}}}).formatRange({num(a)}, {num(b)})", [locale, a, b], "N9a",
                E("sec-collapsenumberrange", "sec-partitionnumberrangepattern"),
                "row N9a (implementation-defined): the range separator's spacing and the collapse of shared affixes",
                per_engine_same=True, impl_reason=LIBRARY_TAKES)
    for opts, a, b in (("", -5, -1), ("", -5, 1), ("", -1, -1.5), ("", -1, 1.5), ('style: "unit", unit: "kilometer"', -5, -1),
                       ('style: "percent"', -0.05, -0.01)):
        add("c", "range", "Intl.NumberFormat.prototype.formatRange",
            f"new Intl.NumberFormat(\"en\", {{{opts}}}).formatRange({num(a)}, {num(b)})", ["en", a, b], "N9b",
            E("sec-collapsenumberrange"), "row N9b: a collapse must not make two ranges print alike")
    for a, b in ((1, 1), (0.9999, 1.0001)):
        add("c", "range", "Intl.NumberFormat.prototype.formatRange", f'new Intl.NumberFormat("de").formatRange({num(a)}, {num(b)})',
            ["de", a, b], "N9c", E("sec-formatapproximately"), "row N9c (data, C5): the approximately sign")
    for opts, a, b in (('style: "unit", unit: "kilometer", unitDisplay: "long"', 3, 5),
                       ('style: "unit", unit: "liter", unitDisplay: "long"', 1, 5),
                       ('style: "currency", currency: "EUR", currencyDisplay: "name"', 1, 5)):
        add("c", "range", "Intl.NumberFormat.prototype.formatRange",
            f"new Intl.NumberFormat(\"en\", {{{opts}}}).formatRange({num(a)}, {num(b)})", ["en", a, b], "N9d",
            E("sec-collapsenumberrange"), "row N9d (implementation-defined): a range written with names, by plural ranges",
            per_engine_same=True, impl_reason=LIBRARY_TAKES)
    for locale in ("pt-PT", "fr", "es"):
        add("c", "range", "Intl.NumberFormat.prototype.formatRange",
            f'new Intl.NumberFormat({js(locale)}, {{style: "currency", currency: "EUR"}}).formatRange(3, 5)',
            [locale, 3, 5], "N9e", E("sec-partitionnumberrangepattern"), "row N9e (data, C5): the range pattern")
    add("c", "range", "Intl.NumberFormat.prototype.formatRangeToParts",
        j('new Intl.NumberFormat("en").formatRangeToParts(3, 5)'), ["en", 3, 5], "N9f",
        E("sec-intl.numberformat.prototype.formatrangetoparts"), "row N9f (same): the range's parts and sources")
    add("c", "range", "Intl.NumberFormat.prototype.formatRange", 'new Intl.NumberFormat("en").formatRange(NaN, 1)', ["en"],
        "N9f", E("sec-intl.numberformat.prototype.formatrange"), "row N9f (same): NaN is a RangeError")
    # N10: options read again after construction.
    spy = ('(function () { var log = []; var o = {}; ["style", "currency", "currencyDisplay", "currencySign", "unit"]'
           '.forEach(function (k) { Object.defineProperty(o, k, {get: function () { log.push(k); return k === "style" ? '
           '"currency" : k === "currency" ? "EUR" : undefined; }}); }); var f = new Intl.NumberFormat("en", o); '
           'log.length = 0; f.format(1); f.formatToParts(1); f.formatRange(1, 2); f.resolvedOptions(); return log.length; })()')
    add("c", "options", "Intl.NumberFormat (reads after construction)", spy, ["en"], "N10",
        E("sec-intl.numberformat", "sec-intl.numberformat.prototype.format"), "row N10: no option is read after construction")
    mutate = ('(function () { var o = {style: "currency", currency: "EUR"}; var f = new Intl.NumberFormat("en", o); '
              'o.currency = "USD"; o.style = "percent"; o.currencySign = "accounting"; return f.format(1); })()')
    add("c", "options", "Intl.NumberFormat (mutated options)", mutate, ["en"], "N10", E("sec-intl.numberformat"),
        "row N10: a mutation of the options bag after construction changes nothing")
    tls = ('(function () { var n = 0; (1).toLocaleString("en", {get style() { n++; return "decimal"; }}); return n; })()')
    add("c", "options", "Number.prototype.toLocaleString (reads)", tls, ["en"], "N10",
        E("sec-intl.numberformat"), "row N10: toLocaleString reads each option once per call")
    # N11 (the bound format) and N12 (the legacy constructor, implementation-defined).
    add("c", "options", "Intl.NumberFormat.prototype.format (bound)",
        '(function (f) { return f.format === f.format; })(new Intl.NumberFormat("en"))', ["en"], "N11",
        E("sec-intl.numberformat.prototype.format"), "row N11: the bound format is created once")
    add("c", "options", "Intl.NumberFormat.prototype.format (bound)",
        '(function (f) { var g = f.format; return [g.name, g.length, g(1234.5)].join(); })(new Intl.NumberFormat("en"))',
        ["en"], "N11", E("sec-intl.numberformat.prototype.format"), "row N11: the bound function's name, length and call")
    legacy = ('(function () { var o = Object.create(Intl.NumberFormat.prototype); var r = Intl.NumberFormat.call(o, "de"); '
              'return [r === o, Intl.NumberFormat.prototype.resolvedOptions.call(r).locale].join(); })()')
    add("c", "options", "Intl.NumberFormat (legacy constructor)", legacy, ["de"], "N12", E("sec-chainnumberformat"),
        "row N12 (implementation-defined): the normative-optional legacy constructor mode, which each engine keeps",
        per_engine={"broiler-vm": 'value: str:"true,de"', "broiler-js": 'value: str:"false,de"'},
        impl_reason="ChainNumberFormat is normative optional; Broiler.VM implements it, so the call answers its "
                    "receiver, and Broiler.JS does not, so the call answers a new NumberFormat; JSD-0082 keeps each "
                    "engine's choice (rows N12 and T5)")
    # N13 (same): resolvedOptions' property order.
    add("c", "options", "Intl.NumberFormat.prototype.resolvedOptions",
        'Object.keys(new Intl.NumberFormat("en", {style: "currency", currency: "EUR", notation: "compact"}).resolvedOptions()).join()',
        ["en"], "N13 M5", E("sec-intl.numberformat.prototype.resolvedoptions"),
        "rows N13 (same) and M5: the table's order, with the digit properties compact notation sets")
    # N14 (data): a locale the host lacks resolves to an available one (the host culture itself is not run here).
    add("c", "options", NF, nf("de-CH", "", num(1234.5)), ["de-CH", 1234.5], "N14 N1b", num_clauses,
        "rows N14 (data, C5) and N1b (data, C5): the shared data's de-CH, not the host's culture")
    # N16: the formatters over many parts (the charge is not observable here).
    add("c", "options", "Intl.ListFormat.prototype.formatToParts",
        'new Intl.ListFormat("en").formatToParts(Array(2000).fill("a")).length', ["en"], "N16",
        E("sec-createpartsfromlist", "sec-formatlisttoparts"),
        "row N16 (engine policy, decision D18): the paths Broiler.VM charges per element and per part; the vector "
        "reaches them, and Broiler.VM's rule holds the charge")
    # N17a (same) and N17c (implementation-defined): astral digits.
    add("c", "range", "Intl.NumberFormat.prototype.format", 'new Intl.NumberFormat("en-u-nu-mathbold").format(35)',
        ["en-u-nu-mathbold", 35], "N17a", E("sec-partitionnumberpattern"), "row N17a (same): astral digits")
    add("c", "range", "Intl.NumberFormat.prototype.formatRange", 'new Intl.NumberFormat("en-u-nu-mathbold").formatRange(3, 5)',
        ["en-u-nu-mathbold", 3, 5], "N17c", E("sec-collapsenumberrange"),
        "row N17c (implementation-defined): the separator's spacing reads code points", per_engine_same=True,
        impl_reason=LIBRARY_TAKES)
    # S1a (same), S1b (data): lists.
    for locale in ("en", "de"):
        for ltype in ("conjunction", "disjunction", "unit"):
            for style in ("long", "short", "narrow"):
                add("c", "list", "Intl.ListFormat.prototype.format",
                    f'new Intl.ListFormat({js(locale)}, {{type: "{ltype}", style: "{style}"}}).format(["A", "B", "C", "D"])',
                    [locale, ltype, style], "S1a", E("sec-formatlist", "sec-Intl.ListFormat-internal-slots"),
                    "row S1a (same): the list patterns")
    for locale in ("es", "fr", "it", "ja", "zh", "pl", "ru", "ar", "he", "xyz"):
        add("c", "list", "Intl.ListFormat.prototype.format", f'new Intl.ListFormat({js(locale)}).format(["A", "B", "C", "D"])',
            [locale], "S1b", E("sec-formatlist"), "row S1b (data, C5): the locale's list patterns")
    add("c", "list", "Intl.ListFormat.prototype.format", 'new Intl.ListFormat("es").format(["A", "y"])', ["es"], "S1b",
        E("sec-formatlist"), "row S1b (data, C5): Spanish's e before i")
    # S2, S3 (same): ListFormat's errors and parts.
    for script in ('new Intl.ListFormat("en").format(["a", 1])', 'new Intl.ListFormat("en").format(null)',
                   'new Intl.ListFormat("en", {type: "bogus"})', 'Intl.ListFormat("en")'):
        add("c", "list", "Intl.ListFormat (errors)", script, ["en"], "S2", E("sec-createstringlistfromiterable",
            "sec-Intl.ListFormat"), "row S2 (same): the errors ECMA-402 states")
    add("c", "list", "Intl.ListFormat.prototype.formatToParts", j('new Intl.ListFormat("en").formatToParts(["a", "b"])'),
        ["en"], "S3", E("sec-formatlisttoparts"), "row S3 (same): the parts")
    # S4a, S4c (same), S4b (data), S5: relative time.
    for locale in ("en", "de"):
        for numeric in ("always", "auto"):
            for value, unit in ((-1, "day"), (1, "day"), (0, "second"), (-0.0, "year"), (1.5, "hour"), (-1234.5, "month")):
                add("c", "relative", "Intl.RelativeTimeFormat.prototype.format",
                    f'new Intl.RelativeTimeFormat({js(locale)}, {{numeric: "{numeric}"}}).format({num(value)}, "{unit}")',
                    [locale, numeric, value, unit], "S4a S7", E("sec-PartitionRelativeTimePattern"),
                    "rows S4a (same) and S7 (same, decision D33 (a): the past pattern writes the value's magnitude)")
    for locale in ("es", "fr", "pl", "ja", "ru", "ar", "xyz"):
        for style in ("long", "narrow"):
            add("c", "relative", "Intl.RelativeTimeFormat.prototype.format",
                f'new Intl.RelativeTimeFormat({js(locale)}, {{style: "{style}"}}).format(-3, "day")', [locale, style],
                "S4b", E("sec-PartitionRelativeTimePattern"), "row S4b (data, C5): the locale's patterns")
    add("c", "relative", "Intl.RelativeTimeFormat.prototype.formatToParts",
        j('new Intl.RelativeTimeFormat("en").formatToParts(-1, "second")'), ["en"], "S4c S7",
        E("sec-FormatRelativeTimeToParts"), "rows S4c (same) and S7: the parts carry the magnitude, no minusSign part")
    for locale, opts, value in (("en", 'numberingSystem: "arab"', 12), ("de", 'numberingSystem: "thai"', -1234.5),
                                ("en-u-nu-arab", "", 12)):
        add("c", "relative", "Intl.RelativeTimeFormat.prototype.format",
            f'new Intl.RelativeTimeFormat({js(locale)}, {{{opts}}}).format({num(value)}, "day")', [locale, value], "S5",
            E("sec-Intl.RelativeTimeFormat"), "row S5: the numberingSystem reaches the inner NumberFormat")
    add("c", "relative", "Intl.RelativeTimeFormat.prototype.formatToParts",
        j('new Intl.RelativeTimeFormat("en", {numberingSystem: "arab"}).formatToParts(12, "day")'), ["en"], "S5",
        E("sec-FormatRelativeTimeToParts"), "row S5, as parts")
    # S6 (same): RelativeTimeFormat's errors.
    for script in ('new Intl.RelativeTimeFormat("en").format(1, "decade")', 'new Intl.RelativeTimeFormat("en").format(Infinity, "day")',
                   'new Intl.RelativeTimeFormat("en").format(1, "days")'):
        add("c", "relative", "Intl.RelativeTimeFormat (errors and plural units)", script, ["en"], "S6",
            E("sec-singularrelativetimeunit"), "row S6 (same): the units ECMA-402 accepts")
    # S8 (data, an adoption) and S9: display names.
    for locale in ("en", "de", "fr", "es", "ja", "zh-Hant"):
        for dtype, code in (("language", "en-GB"), ("language", "de-AT"), ("region", "US"), ("region", "419"),
                            ("script", "Latn"), ("currency", "EUR"), ("calendar", "gregory"), ("dateTimeField", "weekOfYear")):
            add("c", "displaynames", "Intl.DisplayNames.prototype.of",
                f'new Intl.DisplayNames({js(locale)}, {{type: "{dtype}"}}).of({js(code)})', [locale, dtype, code], "S8",
                E("sec-Intl.DisplayNames.prototype.of", "sec-Intl.DisplayNames-internal-slots"),
                "row S8 (data, C5; an X1 adoption): CLDR's names")
    for opts, code in (('type: "language", languageDisplay: "standard"', "en-GB"), ('type: "region", style: "short"', "GB"),
                       ('type: "region", fallback: "none"', "XA"), ('type: "region", fallback: "code"', "XA"),
                       ('type: "currency", fallback: "none"', "XYZ"), ('type: "language", fallback: "none"', "xyz")):
        add("c", "displaynames", "Intl.DisplayNames.prototype.of", f'new Intl.DisplayNames("en", {{{opts}}}).of({js(code)})',
            ["en", code], "S8", E("sec-Intl.DisplayNames.prototype.of"), "row S8: styles and the fallback")
    S9_REASON = ("Intl.DisplayNames.prototype.of names the code CanonicalCodeForDisplayNames returns (the region "
                 "upper case, the script title case, the calendar lower case), so the answer is the name of the "
                 "canonical code, the reference's answer for it")
    for dtype, code in (("region", "us"), ("region", "Us"), ("script", "latn"), ("script", "HANS"), ("calendar", "GREGORY"),
                        ("calendar", "Islamic-Civil"), ("region", "US\n"), ("region", "١٢٣"),
                        ("script", "Latn\n"), ("calendar", "gregory\n"), ("language", "en\n"),
                        ("language", "de-١٢٣"), ("region", "UK")):
        canonical = {"region": code.upper(), "script": code[:1].upper() + code[1:].lower(), "calendar": code.lower()}.get(dtype)
        regular = code.isascii() and code.isalpha() or "-" in code and code.isascii() and "\n" not in code
        add("c", "displaynames", "Intl.DisplayNames.prototype.of",
            f'new Intl.DisplayNames("en", {{type: "{dtype}", fallback: "code"}}).of({js(code)})', ["en", dtype, code], "S9",
            E("sec-canonicalcodefordisplaynames", "sec-Intl.DisplayNames.prototype.of"),
            "row S9: CanonicalCodeForDisplayNames regularizes an ASCII code's case and refuses anything else",
            **(dict(text_ref=f'new Intl.DisplayNames("en", {{type: "{dtype}", fallback: "code"}}).of({js(canonical)})',
                    text_reason=S9_REASON) if regular and canonical not in (None, code) else {}))
    # S10 (same).
    add("c", "displaynames", "Intl.DisplayNames (errors)", 'new Intl.DisplayNames("en")', ["en"], "S10",
        E("sec-Intl.DisplayNames"), "row S10 (same): the type option is required")


# ---- (d) Date-time formatting, intervals, day periods, hour cycles, zone names, Intl over Temporal ---------------

FRIDAY = 1704467229123   # 2024-01-05T15:07:09.123Z, a Friday
SUNDAY = 1704639600000   # 2024-01-07T15:00:00Z
DTF = "Intl.DateTimeFormat.prototype.format"
LIBRARY_TAKES = ("JSD-0082 decision D35: the text leaves this to the implementation, and every implementation-defined "
                 "row keeps Broiler.VM's answer under Broiler.VM's rule, which admits no C3; the shared library takes "
                 "that answer and both engines' plans adopt it; here it is the reference's")


def dtf(locale: str, options: str, value: str = str(FRIDAY), method: str = "format") -> str:
    zone = 'timeZone: "UTC"' if not re.search(r"\btimeZone:", options) else ""
    o = ", ".join(x for x in (options, zone) if x)
    return f"new Intl.DateTimeFormat({js(locale)}, {{{o}}}).{method}({value})"


def cases_d() -> None:
    fmt = E("sec-formatdatetimepattern", "sec-createdatetimeformat")
    # T1 (same): the constructor's reads; T2: Date.prototype.toLocale*'s.
    spy = ('(function () { var log = []; var o = new Proxy({}, {get: function (t, k) { if (typeof k === "string") '
           'log.push(k); return undefined; }}); %s; return log.join(); })()')
    add("d", "options", "Intl.DateTimeFormat (option reads)", spy % 'new Intl.DateTimeFormat("en", o)', ["en"], "T1",
        P("sec-createdatetimeformat"), "row T1 (same): each option read once, in CreateDateTimeFormat's order")
    for method in ("toLocaleString", "toLocaleDateString", "toLocaleTimeString"):
        add("d", "options", f"Date.prototype.{method} (option reads)", spy % f'new Date(0).{method}("en", o)', ["en"], "T2",
            E(f"sup-date.prototype.{method.lower()}") if False else P("sec-createdatetimeformat"),
            f"row T2: {method} makes its DateTimeFormat by CreateDateTimeFormat, which reads each option once")
    for script in ('new Date(0).toLocaleDateString("en", {timeStyle: "short"})',
                   'new Date(0).toLocaleTimeString("en", {dateStyle: "short"})',
                   'new Date(NaN).toLocaleString("en")'):
        add("d", "options", "Date.prototype.toLocale* (required and defaults)", script, ["en"], "T2",
            P("sec-createdatetimeformat"), "row T2: the required and default components of each method")
    for script in (f'new Date({FRIDAY}).toLocaleDateString("en", {{hour: "numeric", timeZone: "UTC"}})',
                   f'new Date({FRIDAY}).toLocaleTimeString("en", {{year: "numeric", timeZone: "UTC"}})'):
        add("d", "options", "Date.prototype.toLocale* (required and defaults)", script, ["en"], "T2",
            P("sec-createdatetimeformat"), "row T2: a component outside the method's own still formats",
            witness=True)
    # T3: a String as the options argument (Broiler.JS's extension, SJL-D-17, decision R5 of bundle sjl-6-001).
    for fmt_string, js_answer in (("yyyy", "2024"), ("yyyy-MM-dd", "2024-01-05")):
        add("d", "options", "Date.prototype.toLocaleDateString (String options)",
            f'new Date({FRIDAY}).toLocaleDateString("en-US", {js(fmt_string)})', ["en-US", fmt_string], "T3",
            P("sec-createdatetimeformat"),
            "row T3 (engine policy): ToObject of a String names no option, so the defaults format the date",
            per_engine={"broiler-js": f"value: str:{quote(js_answer)}"},
            impl_reason="Broiler.JS keeps its .NET custom date format strings as an extension (SJL-D-17, bundle "
                        "sjl-6-001's row R5), so its plan formats the string's pattern; Broiler.VM is compared with "
                        "the reference")
    # T4: the bound format's identity.
    add("d", "options", "Intl.DateTimeFormat.prototype.format (bound)",
        '(function (f) { return f.format === f.format; })(new Intl.DateTimeFormat("en"))', ["en"], "T4",
        E("sec-intl.datetimeformat.prototype.format"), "row T4: the bound format is created once")
    add("d", "options", "Intl.DateTimeFormat.prototype.format (bound)",
        '(function (g) { return [g.name, g.length].join(); })(new Intl.DateTimeFormat("en").format)', ["en"], "T4",
        E("sec-intl.datetimeformat.prototype.format"), "row T4: the bound function's name and length")
    # T5: the normative-optional legacy constructor.
    legacy = ('(function () { var o = Object.create(Intl.DateTimeFormat.prototype); var r = Intl.DateTimeFormat.call(o, "de"); '
              'return [r === o, Intl.DateTimeFormat.prototype.resolvedOptions.call(r).locale].join(); })()')
    add("d", "options", "Intl.DateTimeFormat (legacy constructor)", legacy, ["de"], "T5", E("sec-chaindatetimeformat"),
        "row T5 (implementation-defined): the normative-optional legacy constructor mode, which each engine keeps",
        per_engine={"broiler-vm": 'value: str:"true,de"', "broiler-js": 'value: str:"false,de"'},
        impl_reason="ChainDateTimeFormat is normative optional; Broiler.VM implements it and Broiler.JS does not "
                    "(rows T5 and N12)")
    # T6, T9, T21 (same): resolvedOptions and the parts.
    add("d", "options", "Intl.DateTimeFormat.prototype.resolvedOptions",
        'Object.keys(new Intl.DateTimeFormat("en", {dateStyle: "full", timeStyle: "full", timeZone: "UTC"}).resolvedOptions()).join()',
        ["en"], "T6", E("sec-intl.datetimeformat.prototype.resolvedoptions"), "row T6 (same): the table's order")
    # T7: resolvedOptions' components come from the selected format record.
    for opts, prop in (('hour: "numeric", minute: "numeric"', "minute"), ('hour: "numeric", minute: "numeric"', "hour"),
                       ('month: "numeric", day: "numeric", year: "2-digit"', "year"), ('second: "numeric"', "second")):
        add("d", "resolved", "Intl.DateTimeFormat.prototype.resolvedOptions",
            f'new Intl.DateTimeFormat("en", {{{opts}}}).resolvedOptions().{prop}', ["en", prop], "T7",
            E("sec-intl.datetimeformat.prototype.resolvedoptions"),
            "row T7: each component is the selected format record's field, not the option given")
    # T8 (same, decision D33 (b)): hourCycle and hour12 only with an hour field.
    for opts in ('dateStyle: "short"', 'timeStyle: "short"', 'year: "numeric"'):
        add("d", "resolved", "Intl.DateTimeFormat.prototype.resolvedOptions",
            f'(function (o) {{ return ["hourCycle" in o, "hour12" in o].join(); }})(new Intl.DateTimeFormat("en", {{{opts}}}).resolvedOptions())',
            ["en"], "T8", E("sec-intl.datetimeformat.prototype.resolvedoptions"),
            "row T8 (same; decision D33 (b) reads the test262 files as the arbiter): hourCycle and hour12 are "
            "reported only when the format has an hour field")
    # T10 (data): each locale's hour cycles.
    for locale in ("en-GB", "ja", "ja-JP", "ko", "en-IN", "zh-TW", "de", "en-US"):
        for extra in ("", "hour12: true", "hour12: false"):
            o = ", ".join(x for x in ('hour: "numeric"', extra) if x)
            add("d", "resolved", "Intl.DateTimeFormat.prototype.resolvedOptions",
                f"new Intl.DateTimeFormat({js(locale)}, {{{o}}}).resolvedOptions().hourCycle", [locale], "T10",
                E("sec-intl.datetimeformat-internal-slots"), "row T10 (data, C5): CLDR's time data for the locale")
    # T11 (engine policy) and T27: the default zone, through relations that hold in every host zone.
    add("d", "zones", "Intl.DateTimeFormat (default time zone)",
        'new Intl.DateTimeFormat().resolvedOptions().timeZone === Temporal.Now.timeZoneId()', [], "T11",
        P("sec-createdatetimeformat"),
        "row T11 (engine policy, SJL-D-19): the formatter's default zone is the host zone, the one Temporal.Now reads; "
        "the vector holds in every zone")
    for opts in ('timeZoneName: "long"', 'timeZoneName: "short"', 'dateStyle: "full", timeStyle: "full"'):
        add("d", "zones", "Intl.DateTimeFormat (names of the default zone)",
            f'(function () {{ var f = new Intl.DateTimeFormat("en", {{{opts}}}); var g = new Intl.DateTimeFormat("en", '
            f'{{{opts}, timeZone: f.resolvedOptions().timeZone}}); return f.format({FRIDAY}) === g.format({FRIDAY}); }})()',
            ["en"], "T27", fmt, "row T27: the default zone is named as the resolved zone, in every host zone")
    # T12 (data): U+202F before the day period and the date-time joins.
    for opts in ('timeStyle: "short"', 'timeStyle: "medium"', 'timeStyle: "long"', 'timeStyle: "full"',
                 'dateStyle: "full", timeStyle: "short"', 'dateStyle: "long", timeStyle: "medium"',
                 'dateStyle: "medium", timeStyle: "short"', 'dateStyle: "short", timeStyle: "full"',
                 'hour: "numeric", minute: "numeric"', 'hour: "numeric"', 'weekday: "long", hour: "numeric", minute: "2-digit"'):
        add("d", "patterns", DTF, dtf("en", opts), ["en"], "T12", fmt,
            "row T12 (data, C5): CLDR's patterns, U+202F before the day period and the atTime join (decision D17 (b) "
            "for U+202F)", witness=True)
        add("d", "patterns", "Intl.DateTimeFormat.prototype.formatToParts", j(dtf("en", opts, method="formatToParts")),
            ["en"], "T12", fmt, "row T12, as parts", witness=True)
    for script in (f'new Date({FRIDAY}).toLocaleString("en-US", {{timeZone: "UTC"}})',
                   f'new Date({FRIDAY}).toLocaleTimeString("en-US", {{timeZone: "UTC"}})'):
        add("d", "patterns", "Date.prototype.toLocaleString", script, ["en-US"], "T12", fmt, "row T12, through Date",
            witness=True)
    # T13 (implementation-defined): pattern selection by the best-fit matcher.
    for locale, opts in (("en", 'weekday: "long", day: "numeric"'), ("en", 'minute: "2-digit"'),
                         ("en", 'hour12: false, hour: "numeric", minute: "numeric"'), ("en", 'timeZoneName: "short"'),
                         ("de", 'hourCycle: "h11", timeStyle: "medium"'), ("en", 'year: "numeric", timeZoneName: "long"'),
                         ("en", 'month: "short", day: "numeric", hour: "numeric"')):
        add("d", "patterns", DTF, dtf(locale, opts), [locale], "T13", E("sec-bestfitformatmatcher") + fmt,
            "row T13 (implementation-defined): BestFitFormatMatcher's choice, CLDR's skeletons through the pattern "
            "generator", witness=True, per_engine_same=True, impl_reason=LIBRARY_TAKES)
    # T14 (corrections of Broiler.JS) and T15 (data): German names and layouts.
    for locale in ("de", "de-AT"):
        for opts in ('month: "long"', 'month: "short"', 'month: "narrow"', 'weekday: "long"', 'weekday: "short"',
                     'era: "long", year: "numeric"', 'era: "short", year: "numeric"', 'month: "long", day: "numeric"',
                     'dateStyle: "full"', 'dateStyle: "long"', 'dateStyle: "medium"', 'dateStyle: "short"',
                     'dateStyle: "full", timeStyle: "full"', 'hour: "numeric"', 'month: "numeric", day: "numeric"',
                     'weekday: "short", month: "short", day: "numeric"', 'hour: "numeric", minute: "numeric", second: "numeric", fractionalSecondDigits: 3'):
            add("d", "names", DTF, dtf(locale, opts), [locale], "T14 T15", fmt,
                "rows T14 (names outside English) and T15 (data: German layouts)", witness=True)
    for month in (0, 2, 4):
        add("d", "names", DTF, dtf("de", 'month: "long"', f"Date.UTC(2024, {month}, 5)"), ["de"], "T14", fmt,
            "row T14: German month names, March's included")
    # T16 (data): locales outside Broiler.VM's data.
    for locale in ("fr", "es", "it", "ja", "ko", "zh-TW", "ar-EG", "ru", "en-GB", "en-IN", "pt-BR"):
        for opts in ('dateStyle: "short"', 'dateStyle: "long"', 'month: "long"', 'weekday: "long"', 'hour: "numeric", minute: "numeric"'):
            add("d", "locales", DTF, dtf(locale, opts), [locale], "T16", fmt,
                "row T16 (data, C5): the locale's own patterns and names", witness=True)
    # T17 (data), T18 (implementation-defined), T19, T20: day periods.
    for hour, minute in ((0, 0), (0, 30), (3, 0), (6, 0), (12, 0), (18, 0), (21, 0)):
        add("d", "dayperiods", DTF, dtf("en", 'hour: "numeric", dayPeriod: "long"', f"Date.UTC(2024, 0, 5, {hour}, {minute})"),
            ["en"], "T17", fmt, "row T17 (data, C5): CLDR's en day-period rules", witness=True)
    for locale, opts, hour, minute in (("en", 'dayPeriod: "long"', 12, 0), ("en", 'dayPeriod: "long"', 12, 1),
                                       ("en", 'hour: "numeric", minute: "numeric", dayPeriod: "long"', 12, 1),
                                       ("de", 'dayPeriod: "long"', 0, 0), ("de", 'hour: "numeric", dayPeriod: "long"', 0, 0)):
        add("d", "dayperiods", DTF, dtf(locale, opts, f"Date.UTC(2024, 0, 5, {hour}, {minute})"), [locale], "T18", fmt,
            "row T18 (implementation-defined): noon and midnight", witness=True, per_engine_same=True,
            impl_reason=LIBRARY_TAKES)
    for opts in ('dayPeriod: "long", year: "numeric", month: "short", day: "numeric"', 'dayPeriod: "long", weekday: "long"',
                 'dayPeriod: "long", hour: "numeric", minute: "numeric"',
                 'dayPeriod: "narrow", hour: "numeric", minute: "numeric", second: "numeric"', 'dayPeriod: "long", minute: "numeric"'):
        add("d", "dayperiods", DTF, dtf("en", opts), ["en"], "T19", fmt,
            "row T19: every field of the selected pattern is written with the day period", witness=True)
    add("d", "dayperiods", "Intl.DateTimeFormat.prototype.formatRange",
        f'new Intl.DateTimeFormat("en", {{dayPeriod: "long", hour: "numeric", timeZone: "UTC"}}).formatRange(Date.UTC(2024, 0, 5, 9), Date.UTC(2024, 0, 5, 15))',
        ["en"], "T19", E("sec-partitiondatetimerangepattern"), "row T19, as a range", witness=True)
    for locale, opts in (("en", 'dayPeriod: "long", hour: "numeric", hourCycle: "h23"'), ("de", 'dayPeriod: "long", hour: "numeric"')):
        add("d", "dayperiods", DTF, dtf(locale, opts, "Date.UTC(2024, 0, 5, 15)"), [locale], "T20", fmt,
            "row T20 (implementation-defined): a day period with a 24-hour cycle", witness=True, per_engine_same=True,
            impl_reason=LIBRARY_TAKES)
    add("d", "dayperiods", "Intl.DateTimeFormat.prototype.formatToParts",
        j(dtf("de", 'dayPeriod: "long", hour: "numeric"', "Date.UTC(2024, 0, 5, 15)", "formatToParts")), ["de"], "T20",
        fmt, "row T20, as parts", witness=True, per_engine_same=True, impl_reason=LIBRARY_TAKES)
    # T22 (data), T23 (implementation-defined), T24 (data), T26 (implementation-defined, decision D15): zone names.
    zname = ("(function (f, t) {{ var p = f.formatToParts(t); for (var i = 0; i < p.length; i++) if (p[i].type === "
             '"timeZoneName") return p[i].value; }})(new Intl.DateTimeFormat({loc}, {{timeZone: {zone}, timeZoneName: '
             '{style}}}), {when})')
    summer = "Date.UTC(2024, 6, 15, 12)"
    for loc, zone, style, when in (("de", "America/New_York", "long", str(FRIDAY)), ("de", "Europe/Berlin", "short", str(FRIDAY)),
                                   ("de", "Europe/Berlin", "long", str(FRIDAY)), ("en", "Europe/London", "long", summer),
                                   ("en", "Europe/Dublin", "long", summer), ("en", "Africa/Casablanca", "long", summer),
                                   ("en", "Antarctica/Troll", "long", summer), ("en", "America/New_York", "short", summer),
                                   ("ja", "Asia/Tokyo", "long", str(FRIDAY)), ("fr", "Europe/Paris", "long", summer)):
        add("d", "zones", "Intl.DateTimeFormat (timeZoneName)", zname.format(loc=js(loc), zone=js(zone), style=js(style), when=when),
            [loc, zone, style], "T22", fmt, "row T22 (data, C5): the metazone's specific name")
    for loc, zone, style in (("en", "Europe/Berlin", "longGeneric"), ("en", "Europe/London", "longGeneric"),
                             ("en", "Asia/Kolkata", "longGeneric"), ("en", "America/Sao_Paulo", "longGeneric"),
                             ("en", "America/Argentina/Buenos_Aires", "longGeneric"), ("en", "America/Phoenix", "shortGeneric"),
                             ("de", "America/New_York", "longGeneric"), ("en", "America/New_York", "shortGeneric")):
        add("d", "zones", "Intl.DateTimeFormat (timeZoneName)", zname.format(loc=js(loc), zone=js(zone), style=js(style), when=str(FRIDAY)),
            [loc, zone, style], "T23", fmt, "row T23 (implementation-defined): UTS #35's generic name rules",
            per_engine_same=True, impl_reason=LIBRARY_TAKES)
    for loc, zone, when in (("en", "America/Indiana/Knox", "Date.UTC(1995, 0, 5)"), ("en", "America/Indiana/Knox", "Date.UTC(2010, 0, 5)"),
                            ("en", "Europe/London", "Date.UTC(1970, 0, 5)"), ("en", "Asia/Kolkata", "Date.UTC(1942, 0, 5)"),
                            ("en", "Africa/Monrovia", "Date.UTC(1970, 0, 5)"), ("en", "America/New_York", "Date.UTC(1880, 0, 5)"),
                            ("en", "Europe/Amsterdam", "Date.UTC(1900, 0, 5)"), ("en", "Europe/Dublin", "Date.UTC(1900, 0, 5)")):
        add("d", "zones", "Intl.DateTimeFormat (timeZoneName)", zname.format(loc=js(loc), zone=js(zone), style='"long"', when=when),
            [loc, zone], "T24", fmt, "row T24 (data, C5): the metazone in effect at the date, or the GMT format",
            **(dict(text='value: str:"GMT"',
                    text_reason="Europe/Amsterdam reads Europe/Brussels in the pinned tz release (SJL-D-20), whose "
                                "offset in 1900 is 0:00 with no metazone; UTS #35's localized GMT format writes CLDR's "
                                "gmtZeroFormat, GMT in en, for a zero offset") if zone == "Europe/Amsterdam" else {}))
    for zone, style, name in (("UTC", "long", "Coordinated Universal Time"), ("Etc/GMT", "long", "Coordinated Universal Time"),
                              ("UTC", "short", "UTC"), ("Etc/GMT", "short", "UTC")):
        add("d", "zones", "Intl.DateTimeFormat (timeZoneName)", zname.format(loc='"en"', zone=js(zone), style=js(style), when="0"),
            ["en", zone, style], "T26", fmt,
            "row T26 (implementation-defined, decision D15): the zone is named by its primary, UTC, whatever "
            "identifier is reported",
            per_engine={"broiler-vm": f"value: str:{quote(name)}", "broiler-js": f"value: str:{quote(name)}"},
            impl_reason="decision D15 has the library name a zone by its primary, so Etc/GMT keeps UTC's names "
                        "(\"Coordinated Universal Time\"), both engines' plans")
    # T25, T28, T32, T38 (same): offset zones, the 8.64e15 limit, and errors.
    offset_name = ('(function (f, t) { var p = f.formatToParts(t); for (var i = 0; i < p.length; i++) if (p[i].type === '
                   f'"timeZoneName") return p[i].value; }})(new Intl.DateTimeFormat("en", {{timeZone: "+05:30", '
                   f'timeZoneName: "short"}}), {FRIDAY})')
    for script in (offset_name, dtf("en", 'dateStyle: "short"', "8.64e15"),
                   dtf("en", 'dateStyle: "short"', "8.64e15 + 1"), 'new Intl.DateTimeFormat("en", {timeZone: "Mars/Olympus"})'):
        add("d", "zones", DTF, script, ["en"], "T25", fmt, "row T25 (same): offset zones, the time value's limits, an "
            "unknown zone")
    # T29: ranges that differ in fields the format does not display; T30 (implementation-defined); T31 (data).
    for opts, a, b in (('weekday: "short"', FRIDAY, SUNDAY), ('timeStyle: "short"', "Date.UTC(2024, 0, 5, 10)", "Date.UTC(2024, 0, 7, 10)"),
                       ('hour: "numeric"', "Date.UTC(2024, 0, 5, 10)", "Date.UTC(2024, 0, 7, 10)"),
                       ('month: "long", day: "numeric"', "Date.UTC(2024, 0, 5)", "Date.UTC(2025, 0, 5)")):
        add("d", "ranges", "Intl.DateTimeFormat.prototype.formatRange",
            f'new Intl.DateTimeFormat("en", {{{opts}, timeZone: "UTC"}}).formatRange({a}, {b})', ["en"], "T29",
            E("sec-partitiondatetimerangepattern"),
            "row T29: PartitionDateTimeRangePattern compares every field of the two times, displayed or not",
            witness=True)
    add("d", "ranges", "Intl.DateTimeFormat.prototype.formatRangeToParts",
        j(f'new Intl.DateTimeFormat("en", {{weekday: "short", timeZone: "UTC"}}).formatRangeToParts(Date.UTC(2024, 0, 2), Date.UTC(2024, 0, 3))'),
        ["en"], "T29", E("sec-partitiondatetimerangepattern"), "row T29, as parts")
    for loc, opts, a, b in (("en", 'month: "short", day: "numeric", year: "numeric"', "Date.UTC(2024, 0, 5)", "Date.UTC(2024, 0, 7)"),
                            ("en", 'month: "long", day: "numeric"', "Date.UTC(2024, 0, 5)", "Date.UTC(2024, 1, 7)"),
                            ("en", 'dateStyle: "long"', "Date.UTC(2024, 0, 5)", "Date.UTC(2024, 0, 7)"),
                            ("en", 'hour: "numeric", minute: "numeric", timeZoneName: "short", timeZone: "America/New_York"',
                             "Date.UTC(2024, 2, 10, 6)", "Date.UTC(2024, 2, 10, 8)"),
                            ("en", 'year: "numeric", era: "short"', "Date.UTC(-3, 10, 27)", "Date.UTC(2024, 0, 5)"),
                            ("de", 'dateStyle: "medium"', "Date.UTC(2024, 0, 5)", "Date.UTC(2024, 0, 7)"),
                            ("de", 'day: "2-digit", month: "2-digit", year: "numeric"', "Date.UTC(2024, 0, 5)", "Date.UTC(2024, 0, 7)"),
                            ("en", 'hour: "numeric", minute: "numeric", second: "numeric", fractionalSecondDigits: 2',
                             "Date.UTC(2024, 0, 5, 10, 0, 0, 120)", "Date.UTC(2024, 0, 5, 10, 0, 0, 450)")):
        zone = "" if "timeZone" in opts else ', timeZone: "UTC"'
        add("d", "ranges", "Intl.DateTimeFormat.prototype.formatRange",
            f"new Intl.DateTimeFormat({js(loc)}, {{{opts}{zone}}}).formatRange({a}, {b})", [loc], "T30 T31",
            E("sec-datetimeformat-range-pattern-record", "sec-partitiondatetimerangepattern"),
            "rows T30 (implementation-defined: CLDR's intervalFormats with ICU's fallbacks) and T31 (data: the range "
            "separator)", witness=True, per_engine_same=True, impl_reason=LIBRARY_TAKES)
    # T33 (implementation-defined): calendar resolution.
    for tag in ("en-u-ca-islamic", "en-u-ca-islamic-rgsa"):
        for prop, library in (("calendar", "islamic-tbla"), ("locale", tag)):
            add("d", "calendars", "Intl.DateTimeFormat.prototype.resolvedOptions",
                f"new Intl.DateTimeFormat({js(tag)}).resolvedOptions().{prop}", [tag], "T33",
                E("sec-availablecalendars", "sec-createdatetimeformat"),
                "row T33 (implementation-defined): islamic and islamic-rgsa, which the formatter does not compute, "
                "resolve to islamic-tbla, the locale keeping its keyword",
                per_engine={e: f"value: str:{quote(library)}" for e in ("broiler-vm", "broiler-js")},
                impl_reason="JSD-0082 has the library take Broiler.VM's fallback (row T33), which both engines' plans "
                            "adopt; ICU keeps the calendar")
    for tag in ("en-u-ca-islamicc", "en-u-ca-ethiopic-amete-alem", "en-u-ca-gregorian", "en-u-ca-abc", "en-u-ca-japanese",
                "en-u-ca-chinese", "en-u-ca-hebrew"):
        add("d", "calendars", "Intl.DateTimeFormat.prototype.resolvedOptions",
            f"new Intl.DateTimeFormat({js(tag)}).resolvedOptions().calendar", [tag], "T33",
            E("sec-availablecalendars", "sec-createdatetimeformat"), "row T33: aliases and unknown calendars")
    # T34 (weekdays), T35 (names) and T36 (patterns) of other calendars.
    for cal in ("coptic", "ethiopic", "ethioaa", "hebrew", "indian", "islamic-civil", "islamic-tbla", "islamic-umalqura", "persian",
                "chinese", "dangi", "japanese", "roc", "buddhist"):
        add("d", "calendars", DTF, dtf(f"en-u-ca-{cal}", 'weekday: "long"'), [cal], "T34", fmt,
            "row T34: a day's weekday is the same in every calendar")
        add("d", "calendars", DTF, dtf(f"en-u-ca-{cal}", 'month: "long", day: "numeric", year: "numeric", era: "short"'),
            [cal], "T35", fmt + R("sec-temporal-calendar-date-fields") if False else fmt,
            "row T35: the calendar's month, era and year names")
        add("d", "calendars", DTF, dtf(f"en-u-ca-{cal}", ""), [cal], "T36", fmt, "row T36 (data, C5): the calendar's default pattern")
        add("d", "calendars", DTF, dtf(f"de-u-ca-{cal}", 'dateStyle: "long"'), [cal], "T35 T36", fmt,
            "rows T35 and T36 in German")
    for when, cal in (("Date.UTC(2024, 2, 10)", "hebrew"), ("Date.UTC(2024, 3, 9)", "hebrew"), ("Date.UTC(1911, 0, 5)", "roc"),
                      ("Date.UTC(1860, 0, 5)", "japanese"), ("Date.UTC(622, 2, 22)", "persian"), ("Date.UTC(2023, 2, 22)", "chinese")):
        add("d", "calendars", DTF, dtf(f"en-u-ca-{cal}", 'dateStyle: "full"', when), [cal], "T35", fmt,
            "row T35: leap months, eras and years at their edges",
            **(dict(text='value: str:"Thursday, January 5, 1860 AD"', extra_prov=R("table-calendar-types", "table-eras"),
                    text_reason="the pinned Intl era and month code proposal's calendar table has japanese use the years and "
                                "eras of gregory up to and including 1872-12-31, so 1860-01-05 is in the era ce, AD in "
                                "en, year 1860, in en's full japanese pattern") if cal == "japanese" else {}))
    # T37 (data, decision D21): numbering systems in dates.
    for loc in ("en-u-nu-arab", "en-u-nu-arabext", "en-u-nu-beng", "en-u-nu-deva", "en-u-nu-hanidec", "en-u-nu-thai",
                "en-u-nu-fullwide", "en-u-nu-tamldec", "de-u-nu-arab"):
        add("d", "numbering", DTF, dtf(loc, 'hour: "numeric", minute: "numeric", second: "numeric", fractionalSecondDigits: 3, hourCycle: "h23"'),
            [loc], "T37", fmt, "row T37 (data, C5; decision D21): the numbering system's digits and its own symbols")
        add("d", "numbering", DTF, dtf(loc, 'dateStyle: "short"'), [loc], "T37", fmt, "row T37: the date's digits")
    add("d", "numbering", "Intl.DateTimeFormat (timeZoneName)",
        zname.format(loc='"en-u-nu-arab"', zone='"Asia/Kolkata"', style='"short"', when=str(FRIDAY)), ["en-u-nu-arab"],
        "T37", fmt, "row T37: a GMT offset's digits")
    add("d", "numbering", "Intl.DateTimeFormat.prototype.resolvedOptions",
        'new Intl.DateTimeFormat("ar-EG").resolvedOptions().numberingSystem', ["ar-EG"], "T37",
        E("sec-intl.datetimeformat.prototype.resolvedoptions"), "row T37: ar-EG's default numbering system")
    # T39: a Temporal value's checks, in order; T36, T10 and the Intl over Temporal formats (sources: e8cc03fc).
    for value in ('Temporal.PlainYearMonth.from("2024-03")', 'Temporal.PlainMonthDay.from("03-10")'):
        for opts in ('hour: "numeric"', 'minute: "numeric"', 'weekday: "long"', 'timeStyle: "short"'):
            add("d", "temporal", DTF, f'new Intl.DateTimeFormat("en", {{{opts}}}).format({value})', ["en"], "T39",
                P("sec-temporal-handledatetimetemporalyearmonth", "sec-temporal-handledatetimetemporalmonthday"),
                "row T39: the calendar's RangeError comes before the format's TypeError")
        add("d", "temporal", f"Temporal toLocaleString", f'{value}.toLocaleString("en", {{hour: "numeric"}})', ["en"], "T39",
            P("sec-temporal-handledatetimetemporalyearmonth", "sec-temporal-handledatetimetemporalmonthday"),
            "row T39, through toLocaleString")
    for script in ('Temporal.PlainDate.from("2024-01-05").toLocaleString("en", {calendar: "iso8601", dateStyle: "short"})',
                   'Temporal.PlainYearMonth.from("2024-03").toLocaleString("en", {calendar: "iso8601"})',
                   'Temporal.PlainMonthDay.from("03-10").toLocaleString("en", {calendar: "iso8601"})',
                   'Temporal.PlainDate.from("2024-01-05").toLocaleString("de")',
                   'Temporal.PlainYearMonth.from({year: 2024, month: 3, calendar: "gregory"}).toLocaleString("en", {calendar: "gregory"})',
                   'Temporal.PlainTime.from("15:07").toLocaleString("de")',
                   'new Intl.DateTimeFormat("en", {timeZone: "UTC"}).format(Temporal.Instant.from("2024-01-05T15:07:09Z")) === new Intl.DateTimeFormat("en", {timeZone: "UTC"}).format(1704467229000)'):
        add("d", "temporal", "Intl over Temporal", script, ["en"], "T36", P("sec-temporal-handledatetimevalue", "sec-formatdatetime"),
            "row T36 (data, C5): Intl over Temporal objects, the iso8601 calendar's own patterns")
    # T40 (engine policy): the formatter's charge, reached and not observed.
    add("d", "options", "Intl.DateTimeFormat.prototype.formatToParts",
        f'new Intl.DateTimeFormat("en", {{dateStyle: "full", timeStyle: "full", timeZone: "UTC"}}).formatToParts({FRIDAY}).length',
        ["en"], "T40", fmt, "row T40 (engine policy, decision D18): the path Broiler.VM charges by its output; the "
        "vector reaches it, and Broiler.VM's rule holds the charge")
    # Z8 (a correction of Broiler.VM, decision D15): the identifier as given, case-normalised.
    for given, answer in (("Asia/Calcutta", "Asia/Calcutta"), ("asia/calcutta", "Asia/Calcutta"), ("US/Eastern", "US/Eastern"),
                          ("Europe/Kiev", "Europe/Kiev"), ("America/Buenos_Aires", "America/Buenos_Aires"), ("Etc/UTC", "Etc/UTC"),
                          ("Etc/GMT", "Etc/GMT"), ("GMT", "GMT"), ("Etc/UCT", "Etc/UCT"), ("Universal", "Universal"),
                          ("Zulu", "Zulu"), ("Etc/GMT0", "Etc/GMT0"), ("EST", "EST"), ("CET", "CET"),
                          ("Atlantic/Jan_Mayen", "Atlantic/Jan_Mayen"), ("Antarctica/South_Pole", "Antarctica/South_Pole"),
                          ("Pacific/Enderbury", "Pacific/Enderbury"), ("America/Godthab", "America/Godthab"),
                          ("utc", "UTC"), ("Asia/Kolkata", "Asia/Kolkata")):
        add("d", "zones", "Intl.DateTimeFormat, resolvedOptions().timeZone",
            f'new Intl.DateTimeFormat("en", {{timeZone: {js(given)}}}).resolvedOptions().timeZone', [given], "Z8",
            P("sec-createdatetimeformat") + E("sec-getavailablenamedtimezoneidentifier"),
            "row Z8 (a correction of Broiler.VM at SJL-9.4, decision D15): e8cc03fc's CreateDateTimeFormat sets the "
            "zone to GetAvailableNamedTimeZoneIdentifier's [[Identifier]], the identifier as given in its case",
            text=f"value: str:{quote(answer)}",
            text_reason="decision D15: the pinned text (e8cc03fc's intl.html) reports [[Identifier]], not "
                        "[[PrimaryIdentifier]]; ICU reports its own canonical name")


# ---- (e) Duration formatting ---------------------------------------------------------------------------------------

DFMT = "Intl.DurationFormat.prototype.format"


def df(locale: str, options: str, duration: str, method: str = "format") -> str:
    return f"new Intl.DurationFormat({js(locale)}, {{{options}}}).{method}({duration})"


def cases_e() -> None:
    clauses = E("sec-partitiondurationformatpattern", "sec-getdurationunitoptions")
    units = ("years", "months", "weeks", "days", "hours", "minutes", "seconds", "milliseconds", "microseconds", "nanoseconds")
    pick = "(function (o) { return [" + ", ".join(f'"{u}=" + o.{u} + "/" + o.{u}Display' for u in units[4:8]) + "].join(\" \"); })"
    # U1: the display default of minutes and seconds after a numeric or 2-digit unit.
    for opts in ('style: "long", minutes: "numeric"', 'style: "long", hours: "numeric"', 'style: "long", hours: "2-digit"',
                 'hours: "numeric"', 'minutes: "numeric"'):
        add("e", "unit-options", "Intl.DurationFormat.prototype.resolvedOptions",
            f'{pick}(new Intl.DurationFormat("en", {{{opts}}}).resolvedOptions())', ["en"], "U1", clauses,
            "row U1: after a numeric unit, minutes and seconds take a numeric style and keep the display default "
            "always (GetDurationUnitOptions step 4.b)")
    for opts, durations in (('style: "long", minutes: "numeric"', ("{minutes: 5}", "{seconds: 0}", "{hours: 1, minutes: 2}",
                                                                  "{days: 1}", "{minutes: -5, seconds: -0}")),
                            ('style: "long", hours: "numeric"', ("{hours: 1}", "{minutes: 3}", "{days: 2, hours: 1}", "{hours: 0}")),
                            ('hours: "numeric"', ("{hours: 2, minutes: 3}",)), ('minutes: "numeric"', ("{hours: 2}",))):
        for duration in durations:
            for locale in ("en", "de"):
                add("e", "unit-options", DFMT, df(locale, opts, duration), [locale], "U1", clauses + E("sec-formatnumericunits"),
                    "row U1: the clock writes minutes and seconds, zero included")
    add("e", "unit-options", "Intl.DurationFormat.prototype.formatToParts",
        j(df("en", 'style: "long", minutes: "numeric"', "{hours: 1, minutes: 2}", "formatToParts")), ["en"], "U1",
        clauses, "row U1, as parts")
    add("e", "unit-options", "Temporal.Duration.prototype.toLocaleString",
        'Temporal.Duration.from({minutes: 5}).toLocaleString("en", {style: "long", minutes: "numeric"})', ["en"], "U1",
        clauses, "row U1, through Temporal.Duration")
    # U2: a sub-second unit given numeric is fractional.
    for opts in ('style: "short", milliseconds: "numeric"', 'style: "narrow", microseconds: "numeric", fractionalDigits: 0',
                 'seconds: "numeric", secondsDisplay: "auto", milliseconds: "numeric", millisecondsDisplay: "auto"'):
        pick2 = "(function (o) { return [" + ", ".join(f'"{u}=" + o.{u} + "/" + o.{u}Display' for u in units[6:]) + "].join(\" \"); })"
        add("e", "unit-options", "Intl.DurationFormat.prototype.resolvedOptions",
            f'{pick2}(new Intl.DurationFormat("en", {{{opts}}}).resolvedOptions())', ["en"], "U2",
            clauses + E("sec-isfractionalsecondunitname", "sec-Intl.DurationFormat.prototype.resolvedOptions"),
            "row U2: a fractional unit's display default is auto, and resolvedOptions writes its style numeric")
    # U3: a fractional unit displayed always is a RangeError.
    for opts in ('milliseconds: "numeric", millisecondsDisplay: "always"', 'style: "digital", millisecondsDisplay: "always"',
                 'hours: "numeric", nanosecondsDisplay: "always"',
                 'seconds: "numeric", microseconds: "numeric", microsecondsDisplay: "always"',
                 'style: "long", milliseconds: "numeric", microsecondsDisplay: "always"'):
        add("e", "unit-options", "Intl.DurationFormat (refusals)", f'new Intl.DurationFormat("en", {{{opts}}}).format({{seconds: 1}})',
            ["en"], "U3", E("sec-validatedurationunitstyle", "sec-getdurationunitoptions"),
            "row U3: ValidateDurationUnitStyle refuses a fractional unit displayed always")
    add("e", "unit-options", "Temporal.Duration.prototype.toLocaleString (refusals)",
        'Temporal.Duration.from({hours: 1, minutes: 2, seconds: 3}).toLocaleString("en", {milliseconds: "numeric", millisecondsDisplay: "always"})',
        ["en"], "U3", E("sec-validatedurationunitstyle"), "row U3, through Temporal.Duration")
    # U4, U5, U6, U8 (same): styles and displays.
    for locale in ("en", "de"):
        for style in ("long", "short", "narrow", "digital"):
            for duration in ("{years: 1, months: 2, weeks: 3, days: 4, hours: 5, minutes: 6, seconds: 7, milliseconds: 8, microseconds: 9, nanoseconds: 10}",
                             "{hours: 1, minutes: 2, seconds: 3, milliseconds: 456}", "{days: -1, hours: -2}", "{}"):
                add("e", "styles", DFMT, df(locale, f'style: "{style}"', duration), [locale, style], "U4 U5", clauses,
                    "rows U4 and U5 (same): the style's unit patterns and the list")
    for opts in ('fractionalDigits: 2', 'fractionalDigits: 0', 'style: "digital", fractionalDigits: 9', 'hoursDisplay: "always"'):
        add("e", "styles", DFMT, df("en", opts, "{hours: 1, minutes: 2, seconds: 3, milliseconds: 4, microseconds: 5}"), ["en"],
            "U6 U8", clauses + E("sec-formatnumericunits"), "rows U6 and U8 (same): fractional digits and displays")
    # U7: a property bag, read once in alphabetical order (ToTemporalDuration, e8cc03fc).
    reads = ('(function () { var log = []; var o = {}; ["days", "hours", "microseconds", "milliseconds", "minutes", "months", '
             '"nanoseconds", "seconds", "weeks", "years"].forEach(function (k) { Object.defineProperty(o, k, {get: function () '
             '{ log.push("get " + k); return {valueOf: function () { log.push("valueOf " + k); return 1; }}; }}); }); '
             'new Intl.DurationFormat("en").format(o); return log.join(); })()')
    add("e", "argument", DFMT, reads, ["en"], "U7", P("sec-temporal-totemporalpartialdurationrecord") + E("sec-Intl.DurationFormat.prototype.format"),
        "row U7: each field read once, in alphabetical order, each converted as it is read")
    for getter, label in (('var n = 0; Object.defineProperty(o, "hours", {get: function () { return n++ ? -1 : 1; }})', "1 then -1"),
                          ('var n = 0; Object.defineProperty(o, "hours", {get: function () { return n++ ? 1.5 : 1; }})', "1 then 1.5"),
                          ('var n = 0; Object.defineProperty(o, "hours", {get: function () { if (n++) throw new EvalError("second"); return 1; }})',
                           "a throw on the second read")):
        add("e", "argument", DFMT, f'(function () {{ var o = {{}}; {getter}; return new Intl.DurationFormat("en").format(o); }})()',
            ["en"], "U7", P("sec-temporal-totemporalpartialdurationrecord"),
            f"row U7: a getter that answers {label} is read once")
    for duration in ('{hours: "2"}', "{hours: -0}", "{hours: undefined, minutes: 1}", "{hours: 1.5}", "{}"):
        add("e", "argument", DFMT, df("en", "", duration), ["en"], "U7", P("sec-temporal-totemporalpartialdurationrecord"),
            "row U7: ToIntegerIfIntegral of each field, and a bag with no field")
    # U9: a String argument is parsed, and no getter of Temporal.Duration.prototype is called.
    for duration in ('"PT1H30M"', '"P1Y2M3W4DT5H6M7.008009010S"', '"-PT5M"', '"1 hour"', '""'):
        add("e", "argument", DFMT, df("en", "", duration), ["en"], "U9", P("sec-temporal-totemporalduration"),
            "row U9: ToTemporalDuration parses a String by ParseTemporalDurationString")
    taint = ('(function () { Object.defineProperty(Temporal.Duration.prototype, "years", {get: function () { throw new '
             'EvalError("tainted years"); }}); return new Intl.DurationFormat("en").format("PT1H2M"); })()')
    add("e", "argument", DFMT, taint, ["en"], "U9", P("sec-temporal-totemporalduration") + E("sec-partitiondurationformatpattern"),
        "row U9: the formatter reads the duration's fields, not its properties")
    # U10, U11, U12, U17, U18, U20 (same): errors and resolved options.
    for script in ('new Intl.DurationFormat("en").format({hours: Infinity})', 'new Intl.DurationFormat("en").format({hours: 1, minutes: -1})',
                   'new Intl.DurationFormat("en", {style: "bogus"})', 'Intl.DurationFormat("en")',
                   'Object.keys(new Intl.DurationFormat("en").resolvedOptions()).join()',
                   'Intl.DurationFormat.supportedLocalesOf(["en", "de-AT"]).join()'):
        add("e", "styles", "Intl.DurationFormat (errors and resolvedOptions)", script, ["en"], "U10 U18",
            E("sec-Intl.DurationFormat", "sec-isvalidduration"), "rows U10 and U18 (same)")
    # U13 and U14 (data, decision D34): the clock's separator and two-digit hours, read in CLDR 48.2.0.
    # (the locales come from Broiler.JS's list: digital_cases)
    # U15 (data): unit and list patterns.
    for locale in ("fr", "ja", "es", "ar", "zh-TW"):
        add("e", "data", DFMT, df(locale, 'style: "long"', "{hours: 1, minutes: 2}"), [locale], "U15", clauses,
            "row U15 (data, C5): the locale's unit and list patterns")
    for opts, duration in (('style: "short"', "{nanoseconds: 1}"), ('style: "short"', "{milliseconds: 1, microseconds: 1}"),
                           ('style: "narrow"', "{hours: 1, milliseconds: 1}"), ('style: "narrow"', "{hours: 2, milliseconds: 2}")):
        add("e", "data", DFMT, df("de", opts, duration), ["de"], "U15", clauses, "row U15 (data, C5): German's patterns")
    add("e", "data", "Intl.DurationFormat.supportedLocalesOf", 'Intl.DurationFormat.supportedLocalesOf(["fr", "de-AT", "da", "en"]).join()',
        ["fr", "de-AT", "da", "en"], "U15 L28", E("sec-Intl.DurationFormat.supportedLocalesOf"),
        "rows U15 and L28 (engine policy, decision D7): the available locales",
        per_engine={"broiler-vm": 'value: str:"de-AT,en"'},
        impl_reason="Broiler.VM's available locales are its data's (decision D7); Broiler.JS's are the host's, "
                    "compared with the reference")
    # U16 (data, decision D21): numbering systems in a duration.
    for locale, opts, duration in (("en", 'numberingSystem: "arab"', "{hours: -1, minutes: -2, seconds: -3}"),
                                   ("en", 'numberingSystem: "arab"', "{days: 1000}"),
                                   ("en", 'numberingSystem: "arab", style: "digital"', "{hours: 1, minutes: 2, seconds: 3, milliseconds: 500}"),
                                   ("de", 'numberingSystem: "arab"', "{hours: -1, minutes: -2}"),
                                   ("en-u-nu-arab", "", "{hours: 1}"), ("en-u-nu-arab", 'numberingSystem: "latn"', "{hours: 1}")):
        add("e", "data", DFMT, df(locale, opts, duration), [locale], "U16", clauses,
            "row U16 (data, C5; decision D21): the numbering system's digits and its own CLDR symbols")
    for locale, opts in (("en-u-nu-arab", ""), ("en-u-ca-gregory-nu-latn", ""), ("ar-EG", ""), ("fa", ""), ("en", 'numberingSystem: "abcd"')):
        add("e", "data", "Intl.DurationFormat.prototype.resolvedOptions",
            f'(function (o) {{ return [o.locale, o.numberingSystem].join(); }})(new Intl.DurationFormat({js(locale)}, {{{opts}}}).resolvedOptions())',
            [locale], "U16", E("sec-Intl.DurationFormat", "sec-resolvelocale"), "row U16: the resolved numbering system")
    # U19 (engine policy): the charge of the formats a duration builds, reached and not observed.
    add("e", "styles", "Intl.DurationFormat.prototype.formatToParts",
        'new Intl.DurationFormat("en", {style: "short"}).formatToParts({years: 1, months: 2, weeks: 3, days: 4, hours: 5, minutes: 6, seconds: 7, milliseconds: 8, microseconds: 9, nanoseconds: 10}).length',
        ["en"], "U19", clauses, "row U19 (engine policy, decision D18): the path Broiler.VM charges; the vector reaches "
        "it, and Broiler.VM's rule holds the charge")


# ---- CLDR 48.2.0, read from the cldr-json tarballs (ref:intlcldr@1) --------------------------------------------


class Cldr:
    """The cldr-json 48.2.0 files this generator reads, each tarball checked against its npm integrity first. The
    members are read in memory; nothing is extracted to disk."""

    def __init__(self, directory: Path):
        self.tars: dict[str, tarfile.TarFile] = {}
        for package, integrity in CLDR_TARBALLS.items():
            path = directory / f"{package}-48.2.0.tgz"
            if not path.is_file():
                raise GeneratorError(f"{path} is missing: the generator reads the cldr-json 48.2.0 tarballs")
            digest = "sha512-" + base64.b64encode(hashlib.sha512(path.read_bytes()).digest()).decode("ascii")
            if digest != integrity:
                raise GeneratorError(f"{path.name} has the integrity {digest}, not the registry's {integrity}")
            self.tars[package] = tarfile.open(path, "r:gz")

    def json(self, package: str, member: str) -> dict:
        handle = self.tars[package].extractfile("package/" + member)
        if handle is None:
            raise GeneratorError(f"{package} has no {member}")
        return json.load(handle)

    def has(self, package: str, member: str) -> bool:
        try:
            self.tars[package].getmember("package/" + member)
            return True
        except KeyError:
            return False


# Row N5b (decision D35): the shared library keeps Broiler.VM's composition of a compound unit CLDR names no pattern
# for, as every implementation-defined row keeps Broiler.VM's answer. vm_unit_answer states that composition, Broiler.VM's
# JsNumberFormatter.UnitPattern at its rule commit, over CLDR 48.2.0's units.json, which Broiler.VM's table holds.
N5B_REASON = ("decision D35: every implementation-defined row keeps Broiler.VM's answer, which the shared library "
              "takes and both engines' plans adopt; Broiler.VM composes the numerator's pattern for the width into the "
              "denominator's perUnitPattern or the width's compoundUnitPattern with the denominator's singular name, "
              "each read in the width and then in each wider one, the one-form before the other-form, from CLDR "
              "48.2.0 (ref:intlcldr@1)")


def vm_unit_answer(cldr: Cldr, locale: str, unit: str, width: str, value: int) -> str:
    """Broiler.VM's answer for a unit CLDR 48.2.0 gives no pattern of its own: UnitPattern over its units table (a
    CLDR unit's name without its category; the per entry's compoundUnitPattern), then the pattern with {0} replaced
    by the number. Only a small integer whose plural category is other in en and de is taken, written as its ASCII
    digits, which both locales' decimal format writes for it."""
    if locale not in ("en", "de") or value not in range(2, 1000):
        raise GeneratorError(f"vm_unit_answer states Broiler.VM's composition for en and de and a plural-other integer")
    data = cldr.json("cldr-units-full", f"main/{locale}/units.json")["main"][locale]["units"]
    widths = {"narrow": ("narrow", "short", "long"), "short": ("short", "long"), "long": ("long",)}[width]
    table: dict[tuple[str, str], dict] = {}
    for w in ("long", "short", "narrow"):
        for name, entry in data[w].items():
            if name == "per":
                table[(w, "per")] = entry
            elif "-" in name and isinstance(entry, dict):
                table.setdefault((w, name.split("-", 1)[1]), entry)

    def field_of(identifier: str, key: str) -> str | None:
        for w in widths:
            found = table.get((w, identifier), {}).get(key)
            if found is not None:
                return found
        return None

    def pattern(identifier: str) -> str | None:
        for w in widths:
            entry = table.get((w, identifier), {})
            found = entry.get("unitPattern-count-other")
            if found is not None:
                return found
        return None

    composed = pattern(unit)
    if composed is None:
        per = unit.find("-per-")
        numerator = pattern(unit[:per]) if per > 0 else None
        if numerator is None:
            raise GeneratorError(f"{unit}: Broiler.VM's composition has no numerator pattern in {locale}")
        denominator = unit[per + len("-per-"):]
        per_unit = field_of(denominator, "perUnitPattern")
        if per_unit is not None:
            composed = per_unit.replace("{0}", numerator)
        else:
            compound = field_of("per", "compoundUnitPattern") or "{0}/{1}"
            single = (field_of(denominator, "unitPattern-count-one") or field_of(denominator, "unitPattern-count-other")
                      or denominator).replace("{0}", "").strip()
            composed = compound.replace("{0}", numerator).replace("{1}", single)
    return composed.replace("{0}", str(value))


def broiler_js_locales(cldr: Cldr) -> list[str]:
    """Decision D5's main-data list for Broiler.JS: the languages its CLDR package serves; every language (or its
    CLDR replacement) with CLDR main data, and every CLDR locale (availableLocales' full list or defaultContent), that
    bundle sjl-9-001's locales/union.tsv names; en-US; and und."""
    available = set(cldr.json("cldr-core", "availableLocales.json")["availableLocales"]["full"])
    default_content = set(cldr.json("cldr-core", "defaultContent.json")["defaultContent"])
    aliases = cldr.json("cldr-core", "supplemental/aliases.json")["supplemental"]["metadata"]["alias"]["languageAlias"]
    languages = set(PACKAGE_LANGUAGES)
    named = set()
    lines = [line.rstrip("\n").split("\t") for line in UNION.read_text(encoding="utf-8").splitlines()
             if line and not line.startswith("#")]
    header, rows = lines[0], lines[1:]
    if header[:4] != ["tag", "base", "language", "cldr-main"]:
        raise GeneratorError(f"{UNION} does not have the columns tag, base, language, cldr-main")
    for tag, base, language, main, *_ in rows:
        if main != "yes":
            continue
        replacement = aliases.get(language, {}).get("_replacement", language).split("-")[0]
        languages.add(replacement)
        if base in available or base in default_content:
            named.add(base)
    return sorted(languages | named | {"en-US", "und"})


def hour_cycles(cldr: Cldr, region: str) -> list[str]:
    """HourCyclesOfLocale's list for a lookup region: the region's preferred cycle, then its allowed ones, in UTS #35
    Time Data's order, each a pattern character mapped to its cycle (h and its day-period forms h12, H h23, K h11, k
    h24), without repeats; 001's when the region has no data."""
    data = cldr.json("cldr-core", "supplemental/timeData.json")["supplemental"]["timeData"]
    entry = data.get(region) or data["001"]
    cycles = {"h": "h12", "H": "h23", "K": "h11", "k": "h24"}
    out: list[str] = []
    for symbol in [entry["_preferred"], *entry["_allowed"].split()]:
        cycle = cycles[symbol[0]]
        if cycle not in out:
            out.append(cycle)
    return out


def digital_answer(cldr: Cldr, locale: str) -> str | None:
    """Decision D34: DurationFormat's digital answer for 1:02:03 from CLDR 48.2.0, its latn time separator and its
    durationUnit-type-hms (two-digit hours where it begins hh); None where both are the defaults, ':' and one digit."""
    numbers = cldr.json("cldr-numbers-full", f"main/{locale}/numbers.json")["main"][locale]["numbers"]
    units = cldr.json("cldr-units-full", f"main/{locale}/units.json")["main"][locale]["units"]
    system = numbers["defaultNumberingSystem"]
    separator = numbers[f"symbols-numberSystem-{system}"]["timeSeparator"]
    hms = units["durationUnit-type-hms"]["durationUnitPattern"]
    if separator == ":" and not hms.startswith("hh"):
        return None
    if system != "latn":
        raise GeneratorError(f"{locale}'s default numbering system is {system}; the D34 lines are written for latn")
    hours = "01" if hms.startswith("hh") else "1"
    return f"{hours}{separator}02{separator}03"


def cldr_cases(cldr: Cldr, extra: Sequence[str]) -> None:
    """The cases whose expected value this generator reads from CLDR 48.2.0 itself: rows L19 to L21's hour cycles
    (HourCyclesOfLocale over UTS #35 Time Data), and decision D34's digital lines."""
    for tag, region, rows, why in (
            ("en", "US", "L19 L20 L21", "the likely region, US"), ("en-US", "US", "L19 L20", "US"),
            ("en-GB", "GB", "L19 L20", "GB"), ("ja-JP", "JP", "L19 L20", "JP"), ("ja", "JP", "L19 L20 L21", "the likely region, JP"),
            ("es-419", "419", "L19 L20", "419"), ("ko-KR", "KR", "L19 L20", "KR"), ("zh-TW", "TW", "L19 L20", "TW"),
            ("ar", "EG", "L19 L20 L21", "the likely region, EG"), ("und", "US", "L19 L20 L21", "the likely region, US"),
            ("de", "DE", "L19 L20", "the likely region, DE"), ("fr", "FR", "L19 L20", "the likely region, FR"),
            ("en-US-u-rg-gbzzzz", "GB", "L21", "the -u-rg- override, GB"), ("en-u-rg-sazzzz", "SA", "L21", "the -u-rg- override, SA"),
            ("zh-u-rg-twzzzz", "TW", "L21", "the -u-rg- override, TW"), ("en-u-sd-usca", "US", "L21", "the subdivision's region, US"),
            ("en-u-sd-gbsct", "GB", "L21", "the subdivision's region, GB")):
        cycles = hour_cycles(cldr, region)
        answer = "value: str:" + quote(json.dumps(cycles, separators=(",", ":")))
        add("a", "hour-cycles", "Intl.Locale.prototype.getHourCycles", j(f"new Intl.Locale({js(tag)}).getHourCycles()"), [tag],
            rows, E("sec-hourcyclesoflocale", "sec-regionpreference"),
            f"rows {rows.replace(' ', ', ')}: HourCyclesOfLocale lists the cycles in common use in the lookup region, "
            f"{why}, in descending preference: UTS #35 Time Data's preferred cycle, then its allowed ones",
            cldr=answer, text_reason="HourCyclesOfLocale lists every cycle in common use in the region, in descending "
            "preference; ICU answers the preferred one alone")
    for locale in [*VM_LOCALES, *extra]:
        if not cldr.has("cldr-numbers-full", f"main/{locale}/numbers.json"):
            continue
        answer = digital_answer(cldr, locale)
        if answer is None:
            continue
        add("e", "digital", DFMT, df(locale, 'style: "digital"', "{hours: 1, minutes: 2, seconds: 3}"), [locale], "U13 U14",
            E("sec-Intl.DurationFormat-internal-slots", "sec-formatnumerichours", "sec-formatnumericminutes"),
            "rows U13 and U14 (data, C5; decision D34): [[DigitalFormat]] is CLDR 48.2.0's time separator and "
            "durationUnit-type-hms, two-digit hours where it begins hh", cldr="value: str:" + quote(answer),
            text_reason="decision D34 takes CLDR's durationUnit-type-hms as [[TwoDigitHours]] and its timeSeparator; "
                        "ICU writes one-digit hours whatever the pattern")


# ---- One vector per table family for each extra locale (decision D17) --------------------------------------------


ROW_CLASS_NOTE = {"same": "same", "correction-js": "a correction of Broiler.JS",
                  "correction-vm": "a correction of Broiler.VM", "impl-defined": "implementation-defined",
                  "data": "data, C5", "engine-policy": "engine policy"}


def rows_note(rows: str) -> str:
    """The rows of a note, each with its class: "row L26 (data, C5)", "rows N2b (data, C5) and M5 (...)"."""
    named = [f"{row} ({ROW_CLASS_NOTE[ROWS[row]]})" for row in rows.split()]
    if len(named) == 1:
        return "row " + named[0]
    return "rows " + ", ".join(named[:-1]) + " and " + named[-1]


def family_cases(extra: Sequence[str]) -> None:
    """One vector per table family for each extra locale (decision D17). Each note names the family's rows, the row of
    its table and every row a difference of the family's vector confirms: a percent's fraction digits (M5); a day
    period's flexible rules, its dropping under a 24-hour cycle and its names (T17, T20, T14); and a time pattern's
    U+202F, hour cycle and day-period names (T12, T10, T14)."""
    for tag in extra:
        t = js(tag)
        note = f"{tag}, one of Broiler.JS's extra locales (decision D5): the table family's CLDR data"

        def family(sub: str, op: str, script: str, inputs: Iterable, rows: str, clauses: Iterable[str], what: str,
                   **kwargs) -> Case:
            return add(sub, "families", op, script, inputs, rows, clauses, f"{rows_note(rows)}: {what}; {note}",
                       **kwargs)
        family("a", "Intl.Locale.prototype.maximize", f"new Intl.Locale({t}).maximize().toString()", [tag], "L5",
            E("sec-Intl.Locale.prototype.maximize"), "likely subtags")
        family("a", "Intl.Locale.prototype.getWeekInfo",
            f"(function (w) {{ return JSON.stringify([w.firstDay, w.weekend]); }})(new Intl.Locale({t}).getWeekInfo())", [tag],
            "L26", E("sec-weekinfooflocale"), "week data")
        family("a", "Intl.Locale.prototype.getTextInfo", f"new Intl.Locale({t}).getTextInfo().direction", [tag],
            "L24", E("sec-textdirectionoflocale"), "script metadata")
        family("b", "Intl.PluralRules.prototype.resolvedOptions",
            j(f"new Intl.PluralRules({t}).resolvedOptions().pluralCategories"), [tag], "P3",
            E("sec-intl.pluralrules.prototype.resolvedoptions"), "cardinal plural rules")
        family("b", "Intl.PluralRules.prototype.resolvedOptions",
            j(f'new Intl.PluralRules({t}, {{type: "ordinal"}}).resolvedOptions().pluralCategories'), [tag], "P3",
            E("sec-intl.pluralrules.prototype.resolvedoptions"), "ordinal rules")
        family("b", "Intl.PluralRules.prototype.select", j(f"[0, 1, 2, 3, 5, 11, 21, 101].map(function (n) {{ return new Intl.PluralRules({t}).select(n); }})"),
            [tag], "P3", E("sec-resolveplural", "sec-pluralruleselect"), "cardinal selection")
        family("b", "Intl.PluralRules.prototype.selectRange", f"new Intl.PluralRules({t}).selectRange(1, 2)", [tag],
            "P5b", E("sec-resolvepluralrange", "sec-pluralruleselectrange"), "plural ranges (an X1 adoption)")
        family("c", NF, nf(tag, "", num(-1234567.891)), [tag, -1234567.891], "N1b", E("sec-partitionnumberpattern"),
            "decimal symbols and grouping")
        family("c", NF, nf(tag, 'style: "percent"', num(0.256)), [tag, 0.256], "N2b M5",
            E("sec-partitionnumberpattern", "sec-intl.numberformat"),
            "the percent pattern, with percent's default fraction digits, 0 and 0")
        family("c", NF, nf(tag, 'style: "currency", currency: "EUR"', num(-1234.5)), [tag, -1234.5], "N3c",
            E("sec-partitionnumberpattern"), "the currency pattern")
        family("c", NF, nf(tag, 'style: "currency", currency: "USD", currencyDisplay: "name"', "2"), [tag, 2], "N4",
            E("sec-partitionnumberpattern"), "currency names")
        family("c", NF, nf(tag, 'notation: "compact"', "1234567"), [tag, 1234567], "N6d", E("sec-getnotationsubpattern"),
            "compact patterns")
        family("c", NF, nf(tag, 'style: "unit", unit: "kilometer-per-hour", unitDisplay: "long"', "2"), [tag, 2], "N5d",
            E("sec-partitionnumberpattern"), "unit patterns")
        family("c", "Intl.NumberFormat.prototype.resolvedOptions",
            f"new Intl.NumberFormat({t}).resolvedOptions().numberingSystem", [tag], "N8b",
            E("sec-intl.numberformat.prototype.resolvedoptions"), "the default numbering system")
        family("c", "Intl.ListFormat.prototype.format", f'new Intl.ListFormat({t}).format(["A", "B", "C"])', [tag], "S1b",
            E("sec-formatlist"), "list patterns")
        family("c", "Intl.RelativeTimeFormat.prototype.format", f'new Intl.RelativeTimeFormat({t}).format(-3, "day")',
            [tag], "S4b", E("sec-PartitionRelativeTimePattern"), "relative-time patterns")
        family("c", "Intl.DisplayNames.prototype.of",
            f'[new Intl.DisplayNames({t}, {{type: "language"}}).of("de"), new Intl.DisplayNames({t}, {{type: "region"}}).of("US")].join("|")',
            [tag], "S8", E("sec-Intl.DisplayNames.prototype.of"), "display names (an X1 adoption)")
        family("d", DTF, dtf(tag, 'dateStyle: "full"'), [tag], "T16 T14", E("sec-formatdatetimepattern"),
            "date patterns and names", witness=True)
        family("d", DTF, dtf(tag, 'timeStyle: "short"'), [tag], "T16 T12 T10 T14",
            E("sec-formatdatetimepattern"), "time patterns, with the locale's hour cycle and day-period names",
            witness=True)
        family("d", "Intl.DateTimeFormat.prototype.resolvedOptions",
            f'new Intl.DateTimeFormat({t}, {{hour: "numeric"}}).resolvedOptions().hourCycle', [tag], "T10",
            E("sec-intl.datetimeformat-internal-slots"), "time data")
        family("d", DTF, dtf(tag, 'hour: "numeric", dayPeriod: "long"', "Date.UTC(2024, 0, 5, 15)"), [tag],
            "T17 T20 T16 T14", E("sec-formatdatetimepattern"),
            "day periods, which the locale's 24-hour cycle drops, and their names", witness=True)
        family("d", "Intl.DateTimeFormat.prototype.formatRange",
            f'new Intl.DateTimeFormat({t}, {{dateStyle: "medium", timeZone: "UTC"}}).formatRange(Date.UTC(2024, 0, 5), Date.UTC(2024, 0, 7))',
            [tag], "T30 T31", E("sec-partitiondatetimerangepattern"), "interval formats", witness=True,
            per_engine_same=True, impl_reason=LIBRARY_TAKES)
        family("d", "Intl.DateTimeFormat (timeZoneName)",
            f'(function (p) {{ for (var i = 0; i < p.length; i++) if (p[i].type === "timeZoneName") return p[i].value; }})(new Intl.DateTimeFormat({t}, {{timeZone: "America/New_York", timeZoneName: "long"}}).formatToParts({FRIDAY}))',
            [tag], "T22", E("sec-formatdatetimepattern"), "time zone names")
        family("e", DFMT, df(tag, 'style: "long"', "{hours: 1, minutes: 2, seconds: 3}"), [tag], "U15",
            E("sec-partitiondurationformatpattern"), "the duration's unit and list patterns")


# ---- Running the references -----------------------------------------------------------------------------------


def run_driver(node: str, requests: list[dict], witness: bool, scratch: Path) -> dict[str, str]:
    """Runs intlref.mjs over the requests and answers each one's rendered outcome by its id."""
    name = "witness" if witness else "reference"
    request_path = scratch / f"{name}-requests.json"
    answer_path = scratch / f"{name}-answers.json"
    request_path.write_text(json.dumps(requests), encoding="utf-8")
    command = [node, str(INTLREF), str(CORPUS / "prelude.js"), str(request_path), str(answer_path)]
    if witness:
        command.append("--witness")
    environment = {k: v for k, v in os.environ.items() if k not in ("LANG", "LANGUAGE", "TZ") and not k.startswith("LC_")}
    environment["TZ"] = "UTC"
    process = subprocess.run(command, env=environment, capture_output=True, text=True)
    if process.returncode != 0:
        raise GeneratorError(f"intlref.mjs ({name}) exited with {process.returncode}: {process.stderr.strip()}")
    answers = json.loads(answer_path.read_text(encoding="utf-8"))
    return {a["id"]: a["outcome"] for a in answers["answers"]}


def decode_str(rendered: str) -> str | None:
    """A rendered `value: str:"..."` outcome's String, or None for any other outcome."""
    if not rendered.startswith('value: str:"') or not rendered.endswith('"'):
        return None
    body = rendered[len('value: str:"'):-1]
    units: list[int] = []
    i = 0
    while i < len(body):
        c = body[i]
        if c == "\\":
            nxt = body[i + 1]
            if nxt == "u":
                units.append(int(body[i + 2:i + 6], 16))
                i += 6
                continue
            units.append(ord(nxt))
            i += 2
            continue
        units.append(ord(c))
        i += 1
    return b"".join(u.to_bytes(2, "little") for u in units).decode("utf-16-le", errors="surrogatepass")


def compact(outcome: str) -> str:
    """An outcome as an overruled: entry holds it: the space after its kind removed."""
    kind, _, rest = outcome.partition(": ")
    return f"{kind}:{rest}"


@dataclass
class Vector:
    case: Case
    out: str
    cls: str
    per_engine: dict[str, str]
    prov: list[str]
    notes: list[str]


def restore_nnbsp(case: Case, ref: str, witness: str | None, problems: list[str]) -> tuple[str, bool]:
    """Decision D17 (b): the reference's date-time string with U+202F where the witness's parts have it, when the
    two agree character for character once U+202F is read as U+0020; and whether it was restored."""
    if not case.witness or witness is None:
        return ref, False
    r, w = decode_str(ref), decode_str(witness)
    if r is None or w is None or w == r:
        return ref, False
    if w.replace("\u202f", " ") == r:
        return "value: str:" + quote(w), True
    if "\u202f" in w:
        problems.append(f"{case.script}: the witness answers {witness} and the reference {ref}, which do not "
                        "align; the case cannot be written")
    return ref, False


def resolve(case: Case, ref: str, witness: str | None, extra: dict[str, str], problems: list[str],
            vm: tuple[str, str | None] | None = None, text_answer: str | None = None) -> Vector:
    prov = list(case.clauses) + list(case.extra_prov)
    notes = [case.note]
    source = [REF]
    out, restored = restore_nnbsp(case, ref, witness, problems)
    if restored:
        # No overruled: entry can record the reference's answer, which holds the U+0020 it overrules (an entry holds
        # no space, SCHEMA.md section 6), so the note records it.
        notes.append("U+202F where ICU's pattern has it (decision D17 (b)), its positions from the witness's "
                     "parts; the reference wrote U+0020 there")
    expected = case.text or case.cldr or text_answer or (case.text_fn(ref) if case.text_fn else None)
    if expected is not None:
        if expected != out:
            notes.append(case.text_reason + f" (the reference answered {ref[len('value: '):] if ref.startswith('value: ') else ref})")
            if " " not in compact(ref):
                prov_over = OVERRULED + compact(ref)
            else:
                prov_over = None
            out = expected
            source = [REF_CLDR] if case.cldr is not None else []
            if prov_over:
                source.append(prov_over)
        elif case.cldr is not None:
            source = [REF_CLDR, REF]
    per_engine: dict[str, str] = dict(case.per_engine)
    for engine, answer in extra.items():
        per_engine[engine] = answer
    if case.per_engine_same:
        per_engine = {"broiler-js": out, "broiler-vm": out}
    if case.collapse_same and all(v == out for v in per_engine.values()):
        per_engine = {}
    impl_notes = [case.impl_reason] if per_engine else []
    if vm is not None and "broiler-vm" not in case.per_engine and "broiler-vm" not in case.per_engine_ref:
        vm_answer, _ = restore_nnbsp(case, vm[0], vm[1], problems)
        if vm_answer != out:
            per_engine.setdefault("broiler-js", out)
            per_engine["broiler-vm"] = vm_answer
            impl_notes.append(VM_LIST_REASON.format(moves=", ".join(case.vm_moves)))
    cls = "impl-defined" if per_engine else "normative"
    notes.extend(n for n in impl_notes if n)
    return Vector(case, out, cls, per_engine, prov + source, notes)


# ---- Writing --------------------------------------------------------------------------------------------------


def check_rows(cases: Sequence[Case]) -> list[str]:
    reached = {row for case in cases for row in case.rows}
    return [row for row in BEHAVIOURAL if row not in reached]


def check_clauses(cases: Sequence[Case], spec: Path) -> list[str]:
    """Every spec:ecma402 clause id names an id of the pinned text's sources (the --spec directory, spec/*.html)."""
    ids: set[str] = set()
    for path in sorted((spec / "spec").glob("*.html")):
        ids.update(re.findall(r'id="(sec-[^"]+)"', path.read_text(encoding="utf-8")))
    missing = sorted({c[len(ECMA402):] for case in cases for c in case.clauses if c.startswith(ECMA402)} - ids)
    return missing


def read_divergences(path: Path) -> dict[str, dict[str, str]]:
    """intl-divergences.txt: `reason <key>: <text>` lines, then `<engine> <key> <id>` lines; by id, each engine's text."""
    if not path.exists():
        return {}
    reasons: dict[str, str] = {}
    out: dict[str, dict[str, str]] = {}
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if not line or line.startswith("#"):
            continue
        if line.startswith("reason "):
            key, sep, text = line[len("reason "):].partition(": ")
            if not sep or not text or key in reasons:
                raise GeneratorError(f"{path.name}:{number}: a reason is `reason <key>: <text>`, each key once")
            reasons[key] = text
            continue
        parts = line.split(" ")
        if len(parts) != 3 or parts[0] not in ("broiler-js", "broiler-vm") or parts[1] not in reasons:
            raise GeneratorError(f"{path.name}:{number}: `{line}` is not `<engine> <reason key> <id>`")
        engine, key, identifier = parts
        if engine in out.setdefault(identifier, {}):
            raise GeneratorError(f"{path.name}:{number}: {identifier} is classified twice for {engine}")
        out[identifier][engine] = reasons[key]
    return out


def render_record(vector: Vector, identifier: str, divergence: dict[str, str] | None = None) -> str:
    case = vector.case
    divergence = divergence or {}
    cls = vector.cls
    per_engine = dict(vector.per_engine)
    for engine in divergence:
        if cls == "impl-defined" and engine not in per_engine:
            # A divergence field on an impl-defined vector needs the engine's per-engine field beside it (SCHEMA.md
            # section 5), the expectation its plan commits it to, which the vector's case states. The generator does
            # not take the vector's out for it: an engine compared with out is not thereby committed to it.
            raise GeneratorError(f"{identifier}: an impl-defined vector classified for {engine}, whose case states "
                                 "no per-engine expectation for it (SCHEMA.md section 5)")
    if divergence and cls == "normative":
        cls = "divergence"
    lines = [f"id: {identifier}", "area: intl", f"op: {case.op}",
             "in: " + (" ".join(typed(v) for v in case.inputs) if case.inputs else "undefined"),
             f"out: {vector.out}", f"class: {cls}"]
    for engine in sorted(per_engine):
        lines.append(f"per-engine.{engine}: {per_engine[engine]}")
    for engine in sorted(divergence):
        lines.append(f"divergence.{engine}: {divergence[engine]}")
    lines.append("prov: " + " ".join(dict.fromkeys(vector.prov)))
    lines.append(f"script: {case.script}")
    note = "; ".join(n.rstrip(".") for n in vector.notes if n)
    lines.append("note: " + note)
    lines.append(f"since: {SINCE}")
    for line in lines:
        if "\n" in line or "\r" in line:
            raise GeneratorError(f"{identifier}: a field holds a line break")
        if any(ord(c) > 0x7E or (ord(c) < 0x20) for c in line):
            raise GeneratorError(f"{identifier}: a field holds a character outside printable ASCII: {line!r}")
    return "\n".join(lines)


def write_files(vectors: Sequence[Vector], out: Path, header: dict[str, list[str]],
                divergences: dict[str, dict[str, str]] | None = None) -> dict[str, int]:
    divergences = dict(divergences or {})
    rendered: dict[str, list[str]] = {}
    for sub, (stem, title) in FILES.items():
        records = []
        numbers: dict[str, int] = {}
        for vector in (v for v in vectors if v.case.sub == sub):
            numbers[vector.case.group] = numbers.get(vector.case.group, 0) + 1
            identifier = f"intl/{stem}/{vector.case.group}/{numbers[vector.case.group]:04d}"
            records.append(render_record(vector, identifier, divergences.pop(identifier, None)))
        rendered[sub] = records
    if divergences:
        raise GeneratorError(f"{DIVERGENCES.name} classifies ids the area does not have: {sorted(divergences)[:10]}")
    out.mkdir(parents=True, exist_ok=True)
    counts: dict[str, int] = {}
    for sub, (stem, title) in FILES.items():
        records = rendered[sub]
        text = "\n".join(f"# {line}".rstrip() for line in [f"Generated by scripts/conformance/{GENERATOR}; do not edit by hand.",
                                                             f"The Intl area, sub-area {title}.", *header[sub]])
        text += "\nschema: 1\n\n" + "\n\n".join(records) + "\n"
        (out / f"{stem}.vectors").write_text(text, encoding="utf-8", newline="\n")
        counts[stem] = len(records)
    return counts


HEADER = [
    "Expected values: ICU 78.3 with CLDR 48.0 through Node.js v26.12.0, the reference bundle sjl-9-001's reference.txt",
    "names (ref:intlref@1); the pinned ECMA-402 text's answer where it rules (overruled:); U+202F where ICU's pattern",
    "has it, its positions from Node.js 22.22.0's parts (decision D17 (b)); CLDR 48.2.0 itself for the hour cycles of",
    "UTS #35 Time Data and decision D34's digital lines (ref:intlcldr@1). Each note names the correspondence's rows.",
]


def generate(node: str, witness_node: str, cldr_dir: Path, out: Path, spec: Path | None) -> dict[str, int]:
    CASES.clear()
    cldr = Cldr(cldr_dir)
    # TW_REASON's alias, read where it is stated.
    aliases = cldr.json("cldr-core", "supplemental/aliases.json")["supplemental"]["metadata"]["alias"]["languageAlias"]
    if aliases.get("tw", {}).get("_replacement") != "ak":
        raise GeneratorError("CLDR 48.2.0's aliases.json does not replace tw by ak, as the tw vector's text says")
    locales = broiler_js_locales(cldr)
    extra = [tag for tag in locales if tag not in VM_LOCALES]
    cases_a()
    cases_b()
    cases_c(cldr)
    cases_d()
    cases_e()
    cldr_cases(cldr, extra)
    family_cases(extra)
    cases = list(CASES)
    for case in cases:
        if set(case.rows) & D21_ROWS and "broiler-vm" not in case.per_engine and "broiler-vm" not in case.per_engine_ref \
                and not vm_script(case.script)[1]:
            latn = d21_script(case.script)
            if latn is not None:
                case.per_engine_ref = {"broiler-js": case.script, "broiler-vm": latn}
                case.collapse_same = True
                case.impl_reason = D21_REASON
    missing_rows = check_rows(cases)
    if missing_rows:
        raise GeneratorError(f"no vector reaches the rows {missing_rows}")
    if spec is not None:
        missing = check_clauses(cases, spec)
        if missing:
            raise GeneratorError(f"clause ids the pinned text does not have: {missing}")
    requests = []
    witness_requests = []
    extras: list[dict[str, str]] = []
    for index, case in enumerate(cases):
        requests.append({"id": f"c{index}", "script": case.script})
        if case.witness and "Temporal" not in case.script:
            witness_requests.append({"id": f"c{index}", "script": case.script})
        mine = {}
        for engine, script in case.per_engine_ref.items():
            requests.append({"id": f"c{index}-{engine}", "script": script})
            mine[engine] = f"c{index}-{engine}"
        extras.append(mine)
        if case.text_ref is not None:
            requests.append({"id": f"c{index}-text", "script": case.text_ref})
        if "broiler-vm" not in case.per_engine and "broiler-vm" not in case.per_engine_ref:
            moved, moves = vm_script(case.script)
            if moves:
                case.vm_moves = tuple(moves)
                requests.append({"id": f"c{index}-vm", "script": moved})
                if case.witness and "Temporal" not in moved:
                    witness_requests.append({"id": f"c{index}-vm", "script": moved})
    with tempfile.TemporaryDirectory() as scratch:
        answers = run_driver(node, requests, False, Path(scratch))
        witnessed = run_driver(witness_node, witness_requests, True, Path(scratch)) if witness_requests else {}
    problems: list[str] = []
    vectors = []
    for index, case in enumerate(cases):
        extra_out = {engine: answers[key] for engine, key in extras[index].items()}
        vm = (answers[f"c{index}-vm"], witnessed.get(f"c{index}-vm")) if case.vm_moves else None
        text_answer = answers[f"c{index}-text"] if case.text_ref is not None else None
        vectors.append(resolve(case, answers[f"c{index}"], witnessed.get(f"c{index}"), extra_out, problems, vm,
                               text_answer))
    if problems:
        raise GeneratorError("\n".join(problems))
    header = {sub: HEADER + [f"Broiler.JS's extra locales (decision D5): {', '.join(extra)}."] if sub else HEADER
              for sub in FILES}
    return write_files(vectors, out, header, read_divergences(DIVERGENCES))


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n", 1)[0])
    parser.add_argument("--node", required=True, help="the reference Node.js v26.12.0")
    parser.add_argument("--witness-node", required=True, help="Node.js 22.22.0, the U+202F witness")
    parser.add_argument("--cldr", required=True, help="the directory holding the cldr-json 48.2.0 tarballs")
    parser.add_argument("--out", default=str(DEFAULT_OUT))
    parser.add_argument("--spec", help="a directory holding the pinned ECMA-402's spec/*.html, to check clause ids")
    arguments = parser.parse_args(argv)
    try:
        counts = generate(arguments.node, arguments.witness_node, Path(arguments.cldr), Path(arguments.out),
                          Path(arguments.spec) if arguments.spec else None)
    except GeneratorError as error:
        print(f"{GENERATOR}: {error}", file=sys.stderr)
        return 2
    for stem, count in counts.items():
        print(f"{stem}.vectors: {count} vectors")
    return 0


if __name__ == "__main__":
    sys.exit(main())
