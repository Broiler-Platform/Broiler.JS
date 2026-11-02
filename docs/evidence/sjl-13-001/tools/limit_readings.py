#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Part A's limit readings under decision (f') of decision-rule-revised-2026-11-02-second.md: which inputs the
classification expects accepted are, by a measure taken from their source and not from the library, past a limit's bound.

Usage:
    limit_readings.py write --manifest M --corpus D --test262 T --classification C --nesting V=64,J=512 --tree-depth 10000 --out F
    limit_readings.py check (the same options)
    limit_readings.py measure FILE...

Two measures are taken of an input's text by the lexer below, which knows comments, string literals, template literals
with their substitutions, and regular-expression literals (a `/` read as a literal's start where the token before it ends
no expression), and nothing of the library:

- nesting: the deepest nesting of brackets, `(`, `[`, `{` and a template's `${`, each of which opens a production nested
  in the one around it (a parenthesised expression, an argument list, an array or object literal, a block or body, a
  substitution);
- tree: the longest run of operands joined by one binary operator, the same token each time, of one left-associative
  precedence level (`+ - * % << >> >>> & | ^ && || ?? == != === !== < > <= >=`), each operand a single identifier,
  numeric or string literal token, all at one bracket depth: such a run of n operators is a chain of n binary nodes
  each the left operand of the next, whatever binds its first or last operand, so that the tree holds a path of at least
  n chain nodes.

A text is measured only if the lexer reads it whole and its brackets close in order; otherwise both measures are
`unmeasured` and nothing of the input is listed. An input, variant and configuration is listed, with the limit, its
measure and its bound, if and only if the classification expects it accepted (not `either`), and its nesting measure is
past that configuration's nesting parameter (limit `nesting`), or its tree measure is past the tree-depth constant (limit
`tree`); an input past both is listed twice. The source is the manifest's: a test262 input from the checkout given, a
corpus input from the corpus directory, each checked against the manifest's SHA-256 (a mismatch stops the script). A
variant's text is the input's (a `strict` variant adds a prologue of no bracket or operator). `write` writes the list;
`check` rebuilds it and compares it byte for byte; `measure` prints both measures of each file given.
"""
import argparse
import gzip
import hashlib
import os
import re
import sys

OPERATORS = {"+", "-", "*", "%", "<<", ">>", ">>>", "&", "|", "^", "&&", "||", "??", "==", "!=", "===", "!==",
             "<", ">", "<=", ">="}
PUNCTUATORS = sorted([
    ">>>=", "...", "===", "!==", "**=", "<<=", ">>=", ">>>", "&&=", "||=", "??=",
    "=>", "==", "!=", "<=", ">=", "&&", "||", "??", "?.", "++", "--", "+=", "-=", "*=", "%=", "&=", "|=", "^=", "/=",
    "<<", ">>", "**",
    "{", "}", "(", ")", "[", "]", ";", ",", "<", ">", "+", "-", "*", "%", "&", "|", "^", "!", "~", "?", ":", "=", ".",
    "@", "#", "/",
], key=len, reverse=True)
# The tokens after which a `/` begins a regular-expression literal: every punctuator but those that end an expression,
# and the keywords an expression may follow.
ENDS_EXPRESSION = {")", "]", "}", "++", "--"}
REGEXP_AFTER_WORDS = {"return", "typeof", "instanceof", "in", "of", "new", "delete", "void", "throw", "case", "do",
                      "else", "yield", "await", "extends"}
RESERVED = {"await", "break", "case", "catch", "class", "const", "continue", "debugger", "default", "delete", "do",
            "else", "enum", "export", "extends", "false", "finally", "for", "function", "if", "import", "in",
            "instanceof", "new", "null", "return", "super", "switch", "this", "throw", "true", "try", "typeof", "var",
            "void", "while", "with", "yield", "let", "static", "implements", "interface", "package", "private",
            "protected", "public", "of", "async", "get", "set"}
NUMBER = re.compile(r"(?:0[xXoObB][0-9a-fA-F_]+n?|(?:\d[\d_]*\.?[\d_]*|\.\d[\d_]*)(?:[eE][+-]?\d[\d_]*)?n?)")
WORD = re.compile(r"(?:[\w$\u200c\u200d\u2118\u212e\u309b\u309c\u00b7\u0387\u1369-\u1371\u19da]|\\u\{[0-9a-fA-F]+\}|\\u[0-9a-fA-F]{4}|[^\x00-\x7f\u00a0\u1680\u2000-\u200a\u2028\u2029\u202f\u205f\u3000\ufeff])+",
                  re.UNICODE)
SPACE = re.compile("[ \\t\\v\\f\\r\\n\\u00a0\\u1680\\u2000-\\u200a\\u2028\\u2029\\u202f\\u205f\\u3000\\ufeff]+")
OPENERS = {"(": ")", "[": "]", "{": "}"}


class Unmeasured(Exception):
    pass


def tokens(text):
    """The text's tokens as (kind, value, depth): kind is `word`, `number`, `string`, `regexp`, `punct` or `template`;
    depth is the bracket depth the token stands at. Yields ('depth', n, n) whenever the depth rises to n."""
    i, n = 0, len(text)
    stack = []  # each entry: the closing token, or "`" for a template substitution
    previous = None  # (kind, value) of the last significant token
    if text.startswith("#!"):
        end = text.find("\n")
        i = n if end < 0 else end

    def template(start):
        # Reads template characters from start; returns the index after the closing "`" or after "${", and which.
        j = start
        while j < n:
            c = text[j]
            if c == "\\":
                j += 2
                continue
            if c == "`":
                return j + 1, "end"
            if c == "$" and j + 1 < n and text[j + 1] == "{":
                return j + 2, "substitution"
            j += 1
        raise Unmeasured("unterminated template")

    while i < n:
        m = SPACE.match(text, i)
        if m:
            i = m.end()
            continue
        c = text[i]
        if text.startswith("//", i) or text.startswith("<!--", i) or (text.startswith("-->", i) and _line_start(text, i)):
            end = text.find("\n", i)
            i = n if end < 0 else end
            continue
        if text.startswith("/*", i):
            end = text.find("*/", i + 2)
            if end < 0:
                raise Unmeasured("unterminated comment")
            i = end + 2
            continue
        if c in "'\"":
            j = i + 1
            while True:
                if j >= n:
                    raise Unmeasured("unterminated string")
                d = text[j]
                if d == "\\":
                    j += 2
                    continue
                if d == c:
                    break
                if d in "\n\r":
                    raise Unmeasured("line terminator in string")
                j += 1
            yield ("string", text[i:j + 1], len(stack))
            previous = ("string", None)
            i = j + 1
            continue
        if c == "`":
            j, how = template(i + 1)
            if how == "substitution":
                stack.append("`")
                yield ("depth", len(stack), len(stack))
            yield ("template", None, len(stack))
            previous = ("punct", "${") if how == "substitution" else ("template", None)
            i = j
            continue
        if c == "}" and stack and stack[-1] == "`":
            stack.pop()
            j, how = template(i + 1)
            if how == "substitution":
                stack.append("`")
                previous = ("punct", "${")
            else:
                previous = ("template", None)
            yield ("template", None, len(stack))
            i = j
            continue
        if c == "/" and _regexp_allowed(previous):
            j = i + 1
            in_class = False
            while True:
                if j >= n or text[j] in "\n\r":
                    raise Unmeasured("unterminated regular expression")
                d = text[j]
                if d == "\\":
                    j += 2
                    continue
                if d == "[":
                    in_class = True
                elif d == "]":
                    in_class = False
                elif d == "/" and not in_class:
                    break
                j += 1
            m = WORD.match(text, j + 1)
            j = m.end() if m else j + 1
            yield ("regexp", None, len(stack))
            previous = ("regexp", None)
            i = j
            continue
        m = NUMBER.match(text, i)
        if m and (c.isdigit() or (c == "." and i + 1 < n and text[i + 1].isdigit())):
            yield ("number", m.group(0), len(stack))
            previous = ("number", None)
            i = m.end()
            continue
        m = WORD.match(text, i)
        if m:
            yield ("word", m.group(0), len(stack))
            previous = ("word", m.group(0))
            i = m.end()
            continue
        for p in PUNCTUATORS:
            if text.startswith(p, i):
                break
        else:
            raise Unmeasured("no token at %d" % i)
        if p in OPENERS:
            stack.append(OPENERS[p])
            yield ("depth", len(stack), len(stack))
        elif p in (")", "]", "}"):
            if not stack or stack[-1] != p:
                raise Unmeasured("bracket %s closes nothing open at %d" % (p, i))
            stack.pop()
        yield ("punct", p, len(stack))
        previous = ("punct", p)
        i += len(p)
    if stack:
        raise Unmeasured("brackets left open")


def _line_start(text, i):
    j = i - 1
    while j >= 0 and text[j] in " \t":
        j -= 1
    return j < 0 or text[j] in "\n\r"


def _regexp_allowed(previous):
    if previous is None:
        return True
    kind, value = previous
    if kind == "punct":
        return value not in ENDS_EXPRESSION
    if kind == "word":
        return value in REGEXP_AFTER_WORDS
    return False


def measure(text):
    """(nesting, tree) of a text, or None if the lexer cannot read it whole with its brackets closing in order."""
    nesting = 0
    best = 0
    # The current run: its operator and count; an operator read after an operand and awaiting its right operand; and
    # whether the last token read was an operand. Any other token, or a change of depth, ends the run.
    run_op, run_count, pending, operand_last = None, 0, None, False
    try:
        for kind, value, depth in tokens(text):
            if kind == "depth":
                nesting = max(nesting, value)
            if kind in ("number", "string") or (kind == "word" and value not in RESERVED):
                if pending is not None:
                    if pending == run_op:
                        run_count += 1
                    else:
                        best = max(best, run_count)
                        run_op, run_count = pending, 1
                    pending = None
                elif operand_last:
                    best = max(best, run_count)
                    run_op, run_count = None, 0
                operand_last = True
                continue
            if kind == "punct" and value in OPERATORS and operand_last:
                pending = value
                operand_last = False
                continue
            best = max(best, run_count)
            run_op, run_count, pending, operand_last = None, 0, None, False
    except Unmeasured:
        return None
    return nesting, max(best, run_count)


def read_manifest(path):
    rows = []
    with open(path, encoding="utf-8") as handle:
        columns = None
        for line in handle.read().split("\n"):
            if not line or line.startswith("#"):
                continue
            fields = line.split("\t")
            if columns is None:
                columns = fields
                continue
            rows.append(dict(zip(columns, fields)))
    return rows


def build(args):
    nesting = dict(item.split("=") for item in args.nesting.split(","))
    tree_depth = int(args.tree_depth)
    expected = {}
    with gzip.open(args.classification, "rt", encoding="utf-8") as handle:
        columns = None
        for line in handle:
            if line.startswith("#"):
                continue
            fields = line.rstrip("\n").split("\t")
            if columns is None:
                columns = fields
                continue
            row = dict(zip(columns, fields))
            if row["expect"] == "accepted":
                expected.setdefault(row["id"], []).append((row["variant"], row["config"]))
    floor = min(int(v) for v in nesting.values())
    measured, unmeasured = {}, []
    for row in read_manifest(args.manifest):
        if row["id"] not in expected:
            continue
        if row["origin"] == "test262":
            path = os.path.join(args.test262, row["path"])
        else:
            path = os.path.join(args.corpus, row["path"])
        with open(path, "rb") as handle:
            data = handle.read()
        if hashlib.sha256(data).hexdigest() != row["sha256"]:
            sys.exit("limit_readings: %s (%s) differs from the manifest's SHA-256" % (row["id"], path))
        text = data.decode("utf-8")
        # Neither measure can pass its bound in a text with too few brackets or operators to reach it.
        openers = text.count("(") + text.count("[") + text.count("{")
        operators = max(text.count(ch) for ch in "+-*%<>&|^=?")
        if openers <= floor and operators <= tree_depth:
            continue
        result = measure(text)
        if result is None:
            unmeasured.append(row["id"])
            continue
        measured[row["id"]] = result
    lines = []
    for ident in sorted(measured):
        depth, chain = measured[ident]
        for variant, config in expected[ident]:
            if depth > int(nesting[config]):
                lines.append("\t".join([ident, variant, config, str(depth), nesting[config], "nesting"]))
            if chain > tree_depth:
                lines.append("\t".join([ident, variant, config, str(chain), str(tree_depth), "tree"]))
    header = ["# Part A's limit readings, decision (f') of decision-rule-revised-2026-11-02-second.md; written by",
              "# tools/limit_readings.py from the manifest's inputs and the classification, measured from each input's",
              "# source and not from the library. Never edited by hand. Inputs measured past a floor: %d; unmeasured: %s."
              % (len(measured) + len(unmeasured), ",".join(sorted(unmeasured)) or "none"),
              "id\tvariant\tconfig\tmeasure\tbound\tlimit"]
    return "\n".join(header + lines) + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    sub = parser.add_subparsers(dest="command", required=True)
    for name in ("write", "check"):
        c = sub.add_parser(name)
        c.add_argument("--manifest", required=True)
        c.add_argument("--corpus", required=True)
        c.add_argument("--test262", required=True)
        c.add_argument("--classification", required=True)
        c.add_argument("--nesting", required=True)
        c.add_argument("--tree-depth", required=True)
        c.add_argument("--out", required=True)
    m = sub.add_parser("measure")
    m.add_argument("files", nargs="+")
    args = parser.parse_args()
    if args.command == "measure":
        for path in args.files:
            with open(path, encoding="utf-8") as handle:
                result = measure(handle.read())
            print("%s\t%s" % (path, "unmeasured" if result is None else "nesting=%d\ttree=%d" % result))
        return 0
    text = build(args)
    if args.command == "write":
        with open(args.out, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)
        print("limit_readings: %d lines written" % (text.count("\n") - 4))
        return 0
    with open(args.out, encoding="utf-8") as handle:
        committed = handle.read()
    if committed != text:
        print("limit_readings: %s differs from the list rebuilt" % args.out)
        return 1
    print("limit_readings: %s rebuilt byte for byte (%d lines)" % (args.out, text.count("\n") - 4))
    return 0


if __name__ == "__main__":
    sys.exit(main())
