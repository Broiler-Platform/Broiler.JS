#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Enumerate the locale tags that the passing test262 intl402 files and the reference datasets name.

    python3 scripts/intl/enumerate_locales.py \\
        --report docs/evidence/<bundle>/test262/<run>/test262-results.json.gz \\
        --test262 <test262 checkout> \\
        --broiler-vm <Broiler.VM checkout> --broiler-vm-commit <commit>

It writes docs/evidence/sjl-9-001/locales/ (or --out): `intl402.tsv` and `datasets.tsv`, one row per
locale tag as written and per file that names it; `union.tsv`, one row per tag in BCP 47 case; and
`uncounted.tsv`, the literals that look like tags but are not counted, so that a reader can check
that none of them is a locale the lists miss. Milestone SJL-9 (work package SJL-9.1, task 1.2) uses them for decision D5's list of the locales whose
data Broiler.JS embeds. The report is an input, so the same script runs again on the report taken at
SJL-9's base commit.

WHAT IS READ. The report is read from its path, and every other input from Git objects, never from a
working tree:
- the report, a merged `test262-results.json.gz` of scripts/compliance; a path under test/intl402/ is
  passing when at least one of its cases passed;
- each passing file at the report's suite revision (`suiteRef`), from the test262 repository given;
- the reference datasets' programs at the Broiler.VM commit given (PROGRAMS below);
- CLDR's likelySubtags.json, aliases.json and availableLocales.json from the CLDR 48.2.0 pin archived
  in the same Broiler.VM commit, which say which language subtags CLDR knows.

WHEN A FILE NAMES A LOCALE TAG. A test262 file names every tag its `locale:` metadata lists (test262's
CONTRIBUTING.md: a test that needs specific languages "must declare their requirements by using this
key"); that tag is counted as `metadata`. Beyond it, each file is read as JavaScript tokens: comments
are skipped, and only string literals and template literals without substitutions are read, so a tag
that a program computes at run time (by concatenation, or in a harness file such as testIntl.js) is not
seen. A literal names a locale tag when:
1. it is a structurally valid language tag (ECMA-402's IsStructurallyValidLanguageTag: a Unicode BCP 47
   locale identifier with no duplicate variant and no duplicate singleton), and
2. its language subtag is `und`, or one that CLDR's likely subtags or language aliases name, and
3. one of:
   - `tag`: it has more than one subtag and is not the value of a slot whose name ends in a word for a
     unit, a calendar, a time zone, a currency, a numbering system, a collation or an era (NOT_TAG_SLOT);
   - `argument`: it is, or is an element of an array literal that is, the locales argument of a call
     that takes one (an Intl constructor or the `Constructor` of testWithIntlConstructors,
     getCanonicalLocales, supportedLocalesOf, the toLocale* methods, and localeCompare's second
     argument);
   - `slot`: it is, or is an element of an array literal that is, the value of a variable or property
     whose name says locale or language tag (LOCALE_SLOT);
   - `sibling`: it sits beside a literal counted as `tag`: in the same flat array literal, in the same
     column of an array of array literals, or among the keys, or the values, of the same object literal;
     and its slot's name holds no word for an option value (NOT_SIBLING_SLOT).
   A single subtag needs `argument`, `slot` or `sibling`, because era codes, calendar names, units and
   region codes are often two- or three-letter CLDR language codes.
A literal that meets 1 and 2 but not 3 is written to `uncounted.tsv` with the contexts it was read in.

THE OUTPUT. Every file starts with `#` lines that name its inputs: the report's path and SHA-256, the
run its `run.txt` names, the suite revision, the Broiler.VM commit and the rules above in short. The
rows are sorted, and two runs over the same inputs write the same bytes. `union.tsv`'s `cldr-main` is
`yes` when the tag's language (or its alias's replacement, for a deprecated language) is the language
of a locale CLDR 48.2.0's availableLocales.json lists, that is, when CLDR carries main data for it.
"""

import argparse
import gzip
import hashlib
import json
import os
import re
import subprocess
import sys

REPO = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
DEFAULT_OUT = os.path.join(REPO, "docs", "evidence", "sjl-9-001", "locales")
PREFIX = "test/intl402/"

# The reference datasets of decision D16 and part C of bundle sjl-9-001's rule, by their Broiler.VM path.
PROGRAMS = [
    "src/tests/cldr/dates/dates.js",
    "src/tests/cldr/displaynames/displaynames.js",
    "src/tests/cldr/durations/durations.js",
    "src/tests/cldr/lists/lists.js",
    "src/tests/cldr/locales/locales.js",
    "src/tests/cldr/numbers/numbers.js",
    "src/tests/cldr/plurals/plurals.js",
    "src/tests/cldr/relativetimes/relativetimes.js",
    "src/tests/cldr/segments/segments.js",
    "src/tests/cldr/timezones/timezones.js",
    "src/tests/temporal/intl/temporal-intl.js",
    "src/tests/temporal/calendars-intl/calendar-formats.js",
    "src/tests/temporal/zone-names/zone-names.js",
]
CLDR = "src/tests/cldr/pins/cldr-48.2.0/json/cldr-core/"
CLDR_FILES = {
    "likely": CLDR + "supplemental/likelySubtags.json",
    "aliases": CLDR + "supplemental/aliases.json",
    "available": CLDR + "availableLocales.json",
}

LOCALE_CALLS = {"Collator", "DateTimeFormat", "DisplayNames", "DurationFormat", "ListFormat", "Locale",
                "NumberFormat", "PluralRules", "RelativeTimeFormat", "Segmenter", "Constructor",
                "getCanonicalLocales", "supportedLocalesOf", "toLocaleString", "toLocaleDateString",
                "toLocaleTimeString", "toLocaleUpperCase", "toLocaleLowerCase"}
LOCALE_SLOT = re.compile(r"(?i)(locale|langtag|languagetag|^langs?$|^languages?$|^tags?\d*$)")
NOT_TAG_SLOT = re.compile(r"(?i)(units?|calendars?|calendarIds?|timeZones?|currency|currencies|numberingSystems?|"
                          r"collations?|eras?)$")
NOT_SIBLING_SLOT = re.compile(r"(?i:unit|calendar|timezone|currenc|numbering|collation|region|script|variant|key)"
                              r"|^[Ee]ra|Era")
METADATA = re.compile(r"/\*---(.*?)---\*/", re.S)


# ---------------------------------------------------------------------------------------------------
# Inputs
# ---------------------------------------------------------------------------------------------------

def git(repo, *args, data=None):
    return subprocess.run(["git", "-C", repo, *args], input=data, capture_output=True, check=True).stdout


def resolve(repo, rev):
    return git(repo, "rev-parse", "--verify", rev + "^{commit}").decode().strip()


def read_blobs(repo, rev, paths):
    """Read each path's blob at rev, in one `git cat-file --batch`."""
    out = git(repo, "cat-file", "--batch", data="".join(f"{rev}:{p}\n" for p in paths).encode())
    blobs, pos = {}, 0
    for path in paths:
        end = out.index(b"\n", pos)
        header = out[pos:end].split()
        if len(header) != 3 or header[1] != b"blob":
            raise SystemExit(f"enumerate_locales: {rev}:{path} is not a blob ({out[pos:end].decode()})")
        size = int(header[2])
        blobs[path] = out[end + 1:end + 1 + size].decode("utf-8")
        pos = end + 1 + size + 1
    return blobs


def read_report(path):
    with gzip.open(path, "rb") as stream:
        report = json.load(stream)
    refs = report.get("suiteRefs") or [report.get("suiteRef")]
    if len(set(refs)) != 1 or not refs[0]:
        raise SystemExit("enumerate_locales: the report names no single suite revision")
    statuses = {}
    for case in report["results"]:
        if case["path"].startswith(PREFIX):
            statuses.setdefault(case["path"], set()).add(case["status"])
    passing = sorted(p for p, s in statuses.items() if "passed" in s)
    return refs[0], passing


def describe_report(path):
    """The report's path as the repository names it, its SHA-256, and its run.txt's run and commit."""
    with open(path, "rb") as stream:
        digest = hashlib.sha256(stream.read()).hexdigest()
    absolute = os.path.abspath(path)
    shown = os.path.relpath(absolute, REPO) if absolute.startswith(REPO + os.sep) else os.path.basename(path)
    lines = [f"report: {shown.replace(os.sep, '/')}", f"report sha256: {digest}"]
    run = os.path.join(os.path.dirname(absolute), "run.txt")
    if os.path.exists(run):
        with open(run, encoding="utf-8") as stream:
            for line in stream:
                if line.startswith(("workflow run:", "tested commit:", "includeNegative:")):
                    lines.append("report " + line.strip())
    return lines


# ---------------------------------------------------------------------------------------------------
# Language tags (ECMA-402 6.2.1 IsStructurallyValidLanguageTag, over UTS #35's unicode_bcp47_locale_id)
# ---------------------------------------------------------------------------------------------------

ALPHA = re.compile(r"[a-z]+")
DIGIT = re.compile(r"[0-9]+")
ALNUM = re.compile(r"[a-z0-9]+")


def is_language(s):
    return ALPHA.fullmatch(s) is not None and (2 <= len(s) <= 3 or 5 <= len(s) <= 8)


def is_script(s):
    return ALPHA.fullmatch(s) is not None and len(s) == 4


def is_region(s):
    return (ALPHA.fullmatch(s) is not None and len(s) == 2) or (DIGIT.fullmatch(s) is not None and len(s) == 3)


def is_variant(s):
    return ALNUM.fullmatch(s) is not None and (5 <= len(s) <= 8 or (len(s) == 4 and s[0].isdigit()))


def alnum(s, low, high):
    return ALNUM.fullmatch(s) is not None and low <= len(s) <= high


def language_id(subtags, i):
    """Parse language(-script)?(-region)?(-variant)* from i; return (end, ok)."""
    if i >= len(subtags) or not is_language(subtags[i]):
        return i, False
    i += 1
    if i < len(subtags) and is_script(subtags[i]):
        i += 1
    if i < len(subtags) and is_region(subtags[i]):
        i += 1
    variants = []
    while i < len(subtags) and is_variant(subtags[i]):
        variants.append(subtags[i])
        i += 1
    return i, len(variants) == len(set(variants))


def parse_tag(text):
    """The lowercased subtags of a structurally valid language tag, or None."""
    if not text or not text.isascii():
        return None
    subtags = text.lower().split("-")
    i, ok = language_id(subtags, 0)
    if not ok:
        return None
    singletons = set()
    while i < len(subtags):
        s = subtags[i]
        if len(s) != 1 or not ALNUM.fullmatch(s):
            return None
        if s == "x":
            rest = subtags[i + 1:]
            return subtags if rest and all(alnum(r, 1, 8) for r in rest) else None
        if s in singletons:
            return None
        singletons.add(s)
        i += 1
        start = i
        if s == "u":
            while i < len(subtags) and alnum(subtags[i], 3, 8):
                i += 1
            while i < len(subtags) and len(subtags[i]) == 2 and ALNUM.fullmatch(subtags[i]) and subtags[i][1].isalpha():
                i += 1
                while i < len(subtags) and alnum(subtags[i], 3, 8):
                    i += 1
        elif s == "t":
            if i < len(subtags) and is_language(subtags[i]):
                i, ok = language_id(subtags, i)
                if not ok:
                    return None
            while i < len(subtags) and len(subtags[i]) == 2 and subtags[i][0].isalpha() and subtags[i][1].isdigit():
                field = i
                i += 1
                while i < len(subtags) and alnum(subtags[i], 3, 8):
                    i += 1
                if i == field + 1:
                    return None
        else:
            while i < len(subtags) and alnum(subtags[i], 2, 8):
                i += 1
        if i == start:
            return None
    return subtags


def bcp47_case(subtags):
    """The tag in BCP 47's case conventions: script title case and region upper case before any singleton."""
    out = [subtags[0]]
    in_extension = False
    for s in subtags[1:]:
        if len(s) == 1:
            in_extension = True
        if not in_extension and is_script(s):
            out.append(s.title())
        elif not in_extension and is_region(s):
            out.append(s.upper())
        else:
            out.append(s)
    return "-".join(out)


# ---------------------------------------------------------------------------------------------------
# JavaScript tokens
# ---------------------------------------------------------------------------------------------------

IDENT = re.compile(r"[A-Za-z_$\u0080-\uffff][\w$\u0080-\uffff]*")
NUMBER = re.compile(r"0[xXoObB][0-9a-fA-F_]+n?|\d[\d_]*(?:\.\d*)?(?:[eE][+-]?\d+)?n?|\.\d+(?:[eE][+-]?\d+)?")
PUNCT = re.compile(r"=>|\.\.\.|===|!==|\*\*=|>>>=|<<=|>>=|>>>|&&=|\|\|=|\?\?=|\?\?|\?\.|==|!=|<=|>=|&&|\|\||"
                   r"\+\+|--|<<|>>|\+=|-=|\*=|/=|%=|&=|\|=|\^=|\*\*|[{}()\[\];,<>+\-*/%&|^!~?:=.@#]")
REGEX_AFTER = set("(,=:[!&|?{};+-*%<>~^") | {"=>", "==", "===", "!=", "!==", "&&", "||", "??", "+=", "-="}
REGEX_AFTER_WORDS = {"return", "typeof", "case", "do", "else", "in", "of", "new", "delete", "void", "throw",
                     "instanceof", "yield", "await"}
ESCAPES = {"n": "\n", "t": "\t", "r": "\r", "b": "\b", "f": "\f", "v": "\v"}


def unescape(body):
    """A literal's value; a body whose escapes do not decode is kept as written (it is no tag)."""
    try:
        return decode_escapes(body)
    except (ValueError, IndexError):
        return body


def decode_escapes(body):
    out, i = [], 0
    while i < len(body):
        c = body[i]
        if c != "\\":
            out.append(c)
            i += 1
            continue
        n = body[i + 1] if i + 1 < len(body) else ""
        if n == "u" and body[i + 2:i + 3] == "{":
            end = body.index("}", i + 3)
            out.append(chr(int(body[i + 3:end], 16)))
            i = end + 1
        elif n == "u":
            out.append(chr(int(body[i + 2:i + 6], 16)))
            i += 6
        elif n == "x":
            out.append(chr(int(body[i + 2:i + 4], 16)))
            i += 4
        elif n in "\r\n\u2028\u2029":
            i += 3 if body[i + 1:i + 3] == "\r\n" else 2
        elif n == "0" and not body[i + 2:i + 3].isdigit():
            out.append("\0")
            i += 2
        else:
            out.append(ESCAPES.get(n, n))
            i += 2
    return "".join(out)


def tokens(src):
    """(kind, text) pairs: 'str' (a literal's value), 'ident', 'num', 'punct', 'regex', 'tpl' (a template
    part next to a substitution, whose value is not read), 'other'."""
    out, i, n = [], 0, len(src)
    braces = 0
    templates = []  # the brace depth at which each open template's substitution closes
    while i < n:
        c = src[i]
        if c.isspace() or c == "\ufeff":
            i += 1
        elif src.startswith("//", i):
            end = src.find("\n", i)
            i = n if end < 0 else end
        elif src.startswith("/*", i):
            end = src.find("*/", i + 2)
            i = n if end < 0 else end + 2
        elif c in "\"'":
            j = i + 1
            while j < n and src[j] != c:
                j += 2 if src[j] == "\\" else 1
            out.append(("str", unescape(src[i + 1:j])))
            i = j + 1
        elif c == "`" or (c == "}" and templates and templates[-1] == braces):
            if c == "}":
                templates.pop()
            j = i + 1
            while j < n and src[j] != "`" and not src.startswith("${", j):
                j += 2 if src[j] == "\\" else 1
            closed = j >= n or src[j] == "`"
            whole = c == "`" and closed
            out.append(("str", unescape(src[i + 1:j])) if whole else ("tpl", ""))
            if closed:
                i = j + 1
            else:
                templates.append(braces)
                i = j + 2
        elif c == "/":
            prev = out[-1] if out else None
            regex = prev is None or (prev[0] == "punct" and prev[1] in REGEX_AFTER) or \
                (prev[0] == "ident" and prev[1] in REGEX_AFTER_WORDS)
            if not regex:
                m = PUNCT.match(src, i)
                out.append(("punct", m.group()))
                i = m.end()
                continue
            j, in_class = i + 1, False
            while j < n and src[j] != "\n":
                if src[j] == "\\":
                    j += 2
                    continue
                if src[j] == "[":
                    in_class = True
                elif src[j] == "]":
                    in_class = False
                elif src[j] == "/" and not in_class:
                    break
                j += 1
            j += 1
            while j < n and (src[j].isalnum() or src[j] == "_"):
                j += 1
            out.append(("regex", src[i:j]))
            i = j
        else:
            m = IDENT.match(src, i) or NUMBER.match(src, i)
            if m:
                out.append(("ident" if m.re is IDENT else "num", m.group()))
                i = m.end()
                continue
            m = PUNCT.match(src, i)
            if m:
                t = m.group()
                if t == "{":
                    braces += 1
                elif t == "}":
                    braces -= 1
                out.append(("punct", t))
                i = m.end()
                continue
            out.append(("other", c))
            i += 1
    return out


class Frame:
    """An open bracket: '(' with its callee, '[' as an array literal or an index, '{' as an object literal
    or a block. `element` counts the commas at its level, `value` is set after an object's ':'."""

    def __init__(self, kind, at, callee=None, literal=False, name=None, parent=None):
        self.kind, self.at, self.callee, self.literal, self.name, self.parent = kind, at, callee, literal, name, parent
        self.element = 0
        self.value = False


def slot_name(toks, k, frame):
    """The name a value at token k is given: `name = <value>` or `name: <value>` in an object literal."""
    prev = toks[k - 1] if k else None
    if prev and prev == ("punct", "=") and k >= 2 and toks[k - 2][0] == "ident":
        return toks[k - 2][1]
    if prev and prev == ("punct", ":") and frame is not None and frame.kind == "{" and frame.literal and \
            k >= 3 and toks[k - 2][0] in ("ident", "str", "num") and toks[k - 3] in (("punct", "{"), ("punct", ",")):
        return toks[k - 2][1]
    return None


ITERATORS = {"forEach", "map", "filter", "some", "every", "flatMap", "find"}


def closers(toks):
    """The index of the bracket that closes each '(', '[' and '{'."""
    pairs, stack = {}, []
    for k, (kind, text) in enumerate(toks):
        if kind == "punct" and text in ("(", "[", "{"):
            stack.append(k)
        elif kind == "punct" and text in (")", "]", "}") and stack:
            pairs[stack.pop()] = k
    return pairs


def element_name(toks, k, close):
    """The name each element of the array literal at k is given: the loop variable of `for (x of [...])`,
    or the first parameter of the callback in `[...].forEach(function (x) ...)` or `[...].map((x) => ...)`."""
    if k >= 2 and toks[k - 1] == ("ident", "of") and toks[k - 2][0] == "ident":
        return toks[k - 2][1]
    after = toks[close + 1:close + 9] if close is not None else []
    if len(after) < 5 or after[0] != ("punct", ".") or after[1][0] != "ident" or after[1][1] not in ITERATORS or \
            after[2] != ("punct", "("):
        return None
    rest = after[3:]
    if rest[0] == ("ident", "function"):
        rest = rest[1:] if rest[1] == ("punct", "(") else rest[2:]
        return rest[1][1] if len(rest) > 1 and rest[0] == ("punct", "(") and rest[1][0] == "ident" else None
    if rest[0] == ("punct", "(") and rest[1][0] == "ident":
        return rest[1][1]
    if rest[0][0] == "ident" and rest[1] == ("punct", "=>"):
        return rest[0][1]
    return None


def literals(src):
    """Every literal with its context: (value, context) where context is a dict with 'call' (callee and
    argument index), 'slot' (the name it is given) and 'group' (the key of the literals it stands beside)."""
    toks = tokens(src)
    close = closers(toks)
    stack, found = [], []
    for k, (kind, text) in enumerate(toks):
        frame = stack[-1] if stack else None
        if kind == "punct" and text in ("(", "[", "{"):
            prev = toks[k - 1] if k else None
            if text == "(":
                parts, c = [], k - 1
                while c >= 0 and (toks[c][0] == "ident" or toks[c] in (("punct", "."), ("punct", "?."))):
                    parts.append(toks[c][1])
                    c -= 1
                stack.append(Frame("(", k, callee="".join(reversed(parts)), parent=frame))
            elif text == "[":
                index = prev is not None and ((prev[0] in ("ident", "str", "num") and prev[1] not in REGEX_AFTER_WORDS)
                                              or prev in (("punct", ")"), ("punct", "]")))
                name = None if index else (slot_name(toks, k, frame) or element_name(toks, k, close.get(k)))
                stack.append(Frame("[", k, literal=not index, name=name, parent=frame))
            else:
                literal = prev is None or (prev[0] == "punct" and prev[1] in ("(", ",", "=", ":", "[", "?", "||", "&&", "??")) \
                    or prev == ("ident", "return")
                stack.append(Frame("{", k, literal=literal, name=slot_name(toks, k, frame), parent=frame))
            continue
        if kind == "punct" and text in (")", "]", "}"):
            if stack:
                stack.pop()
            continue
        if kind == "punct" and text == "," and frame is not None:
            frame.element += 1
            frame.value = False
            continue
        if kind == "punct" and text == ":" and frame is not None and frame.kind == "{" and frame.literal:
            frame.value = True
            continue
        if kind != "str":
            continue
        context = {"call": None, "slot": slot_name(toks, k, frame), "group": None}
        # Climb through array literals to the call or the name the outermost array is given.
        f = frame
        while f is not None and f.kind == "[" and f.literal and context["slot"] is None:
            context["slot"] = f.name
            if context["slot"] is None and f.parent is not None and f.parent.kind == "(":
                context["call"] = (f.parent.callee, f.parent.element)
            f = f.parent
        if frame is not None and frame.kind == "(":
            context["call"] = (frame.callee, frame.element)
        # Siblings: a flat array, a column of an array of arrays, or an object's keys or its values.
        if frame is not None and frame.kind == "[" and frame.literal:
            parent = frame.parent
            if parent is not None and parent.kind == "[" and parent.literal:
                context["group"] = ("column", parent.at, frame.element)
            else:
                context["group"] = ("array", frame.at)
        elif frame is not None and frame.kind == "{" and frame.literal:
            context["group"] = ("object", frame.at, "value" if frame.value else "key")
        found.append((text, context))
    return found


# ---------------------------------------------------------------------------------------------------
# Counting
# ---------------------------------------------------------------------------------------------------

def is_locale_call(call):
    if call is None:
        return False
    callee, argument = call
    name = callee.split(".")[-1]
    if name == "localeCompare":
        return argument == 1
    return name in LOCALE_CALLS and argument == 0


def metadata_tags(src):
    """The tags of a test262 file's `locale:` metadata, as a YAML flow list or a block list."""
    m = METADATA.search(src)
    if not m:
        return []
    lines = m.group(1).split("\n")
    for i, line in enumerate(lines):
        if not re.match(r"locale:", line):
            continue
        rest = line[len("locale:"):].strip()
        if rest.startswith("["):
            items = rest.strip("[]").split(",")
        else:
            items = []
            for item in lines[i + 1:]:
                if not re.match(r"\s+-\s", item):
                    break
                items.append(item.strip()[1:])
        return [item.strip().strip("'\"") for item in items if item.strip()]
    return []


def describe_context(context):
    call = context["call"]
    return "slot=%s call=%s group=%s" % (context["slot"] or "-", "%s#%d" % call if call else "-",
                                         context["group"][0] if context["group"] else "-")


def tags_named(src, languages):
    """({tag as written: set of ways it is counted}, {literal not counted: set of its contexts}) for a file."""
    candidates = []
    for value, context in literals(src):
        subtags = parse_tag(value)
        if subtags is None or subtags[0] not in languages:
            continue
        candidates.append((value, subtags, context))
    counted = {}
    for value in metadata_tags(src):
        subtags = parse_tag(value)
        if subtags is not None and subtags[0] in languages:
            counted.setdefault(value, set()).add("metadata")
    groups = set()
    for value, subtags, context in candidates:
        if len(subtags) > 1 and not NOT_TAG_SLOT.search(context["slot"] or ""):
            counted.setdefault(value, set()).add("tag")
            if context["group"] is not None:
                groups.add(context["group"])
    uncounted = {}
    for value, subtags, context in candidates:
        slot = context["slot"] or ""
        if is_locale_call(context["call"]):
            counted.setdefault(value, set()).add("argument")
        elif LOCALE_SLOT.search(slot):
            counted.setdefault(value, set()).add("slot")
        elif context["group"] in groups and not NOT_SIBLING_SLOT.search(slot):
            counted.setdefault(value, set()).add("sibling")
        elif len(subtags) == 1 or NOT_TAG_SLOT.search(slot):
            uncounted.setdefault(value, set()).add(describe_context(context))
    return counted, {value: contexts for value, contexts in uncounted.items() if value not in counted}


def cldr_languages(blobs):
    likely = json.loads(blobs[CLDR_FILES["likely"]])["supplemental"]["likelySubtags"]
    aliases = json.loads(blobs[CLDR_FILES["aliases"]])["supplemental"]["metadata"]["alias"]["languageAlias"]
    available = json.loads(blobs[CLDR_FILES["available"]])["availableLocales"]
    languages = {"und"}
    for key, value in likely.items():
        languages.add(re.split("[-_]", key)[0].lower())
        languages.add(re.split("[-_]", value)[0].lower())
    replacement = {}
    for key, value in aliases.items():
        first = re.split("[-_]", key)[0].lower()
        languages.add(first)
        if re.fullmatch("[a-z]{2,3}", key):
            replacement[key] = re.split("[-_ ]", value["_replacement"])[0].lower()
    main = {re.split("[-_]", tag)[0].lower() for kind in sorted(available) for tag in available[kind]}
    return languages, replacement, main


def write(path, header, columns, rows):
    with open(path, "w", encoding="utf-8", newline="\n") as stream:
        for line in header:
            stream.write(("# " + line).rstrip() + "\n")
        stream.write("\t".join(columns) + "\n")
        for row in rows:
            stream.write("\t".join(row) + "\n")


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("--report", required=True, help="a merged test262-results.json.gz")
    parser.add_argument("--test262", required=True, help="a test262 repository holding the report's suite revision")
    parser.add_argument("--broiler-vm", required=True, help="a Broiler.VM repository")
    parser.add_argument("--broiler-vm-commit", required=True, help="the Broiler.VM commit the datasets are read at")
    parser.add_argument("--out", default=DEFAULT_OUT, help="the output directory")
    args = parser.parse_args(argv)

    suite, passing = read_report(args.report)
    suite = resolve(args.test262, suite)
    vm = resolve(args.broiler_vm, args.broiler_vm_commit)
    tests = read_blobs(args.test262, suite, passing)
    vm_blobs = read_blobs(args.broiler_vm, vm, PROGRAMS + sorted(CLDR_FILES.values()))
    languages, replacement, main_data = cldr_languages(vm_blobs)

    rows = {"intl402": [], "datasets": []}
    skipped = []
    for source, files in (("intl402", tests), ("datasets", {p: vm_blobs[p] for p in PROGRAMS})):
        for path in sorted(files):
            counted, uncounted = tags_named(files[path], languages)
            for tag, ways in counted.items():
                rows[source].append((tag, path, ",".join(sorted(ways))))
            for literal, contexts in uncounted.items():
                skipped.append((literal, path, "; ".join(sorted(contexts))))

    def order(row):
        return (bcp47_case(parse_tag(row[0])), row[0], row[1])

    union = {}
    skipped.sort(key=order)
    for source in ("intl402", "datasets"):
        rows[source].sort(key=order)
        for tag, path, _ in rows[source]:
            entry = union.setdefault(bcp47_case(parse_tag(tag)), {"intl402": set(), "datasets": set()})
            entry[source].add(path)

    rules = [
        "rules: a test262 file names the tags of its `locale:` metadata (`metadata`); and a literal (a",
        "string, or a template without substitutions) names a locale tag when it is a structurally valid",
        "language tag (ECMA-402 IsStructurallyValidLanguageTag), its language subtag is und or one CLDR",
        "48.2.0's likely subtags or language aliases name, and it is counted as `tag` (more than one",
        "subtag, outside a unit, calendar, time-zone, currency, numbering-system, collation or era slot),",
        "`argument` (the locales argument of an Intl constructor, getCanonicalLocales, supportedLocalesOf,",
        "a toLocale* method or localeCompare), `slot` (the value of a variable or property named for a",
        "locale or a language tag) or `sibling` (beside a `tag` literal in the same array, table column or",
        "object). Tags computed at run time, and the harness files, are not read. See",
        "scripts/intl/enumerate_locales.py.",
    ]
    common = [
        "generated by scripts/intl/enumerate_locales.py; two runs over the same inputs write the same bytes",
        *describe_report(args.report),
        f"suite revision: {suite} (test262)",
        "passing: a path under test/intl402/ at least one of whose cases the report records as passed",
        f"Broiler.VM commit: {vm} (the datasets' programs and the CLDR 48.2.0 pin's cldr-core files)",
        *rules,
    ]
    os.makedirs(args.out, exist_ok=True)
    write(os.path.join(args.out, "intl402.tsv"),
          ["Locale tags the passing test262 files under test/intl402/ name, one row per tag as written",
           "and per file that names it.", *common],
          ["tag", "file", "counted-as"], rows["intl402"])
    write(os.path.join(args.out, "datasets.tsv"),
          ["Locale tags the reference datasets' programs name, one row per tag as written and per",
           "program (Broiler.VM paths): " + ", ".join(p.split("/")[-1] for p in PROGRAMS) + ".", *common],
          ["tag", "file", "counted-as"], rows["datasets"])
    union_rows = []
    for tag in sorted(union):
        subtags = tag.split("-")
        singleton = next((i for i, s in enumerate(subtags) if len(s) == 1), len(subtags))
        language = subtags[0]
        data = "yes" if replacement.get(language, language) in main_data else "no"
        union_rows.append((tag, "-".join(subtags[:singleton]), language, data,
                           str(len(union[tag]["intl402"])), str(len(union[tag]["datasets"]))))
    write(os.path.join(args.out, "union.tsv"),
          ["Every locale tag of intl402.tsv and datasets.tsv, in BCP 47 case (language lower case, script",
           "title case, region upper case, the rest lower case; no alias replaced), with its base name (the",
           "subtags before any singleton), its language, whether CLDR 48.2.0 carries main data for that",
           "language (`cldr-main`: the language, or its replacement when CLDR deprecates it, is the language",
           "of a locale in availableLocales.json), and how many intl402 files and dataset programs name it.",
           *common],
          ["tag", "base", "language", "cldr-main", "intl402-files", "dataset-programs"], union_rows)
    write(os.path.join(args.out, "uncounted.tsv"),
          ["Literals of the same files that are structurally valid tags with a CLDR language subtag but",
           "that no rule counts (era codes, calendar names, units, region codes, values compared with a",
           "result), one row per literal as written and per file, with the contexts it was read in: the",
           "slot it is the value of, the call and argument it is passed to, and the kind of literal group",
           "it sits in. A literal a file also counts is not listed. It is here so that a reader can check",
           "that none is a locale the other files miss.", *common],
          ["literal", "file", "contexts"], skipped)
    return 0


if __name__ == "__main__":
    sys.exit(main())
