#!/usr/bin/env python3
"""control-edit.py <c|e> <tree>: the control's edit, in place, to a scratch copy of the after commit (never merged).

c: part C's control. In JSRegExp.Broiler.cs, RunMatch, the adapter answers no match where Broiler.Regex's match
   reports LimitExceeded (frames or trail): the statement that throws the RangeError naming the ceiling is replaced by
   'return RegexMatchData.NoMatch;'.
e: part E's control. One deleted member re-added: JSRegExp's BuildFlagsString, as the base commit 5429c406 has it
   (JSRegExp.cs, its declaration from 'private static string BuildFlagsString(' to its closing brace), inserted before
   the class's closing brace in the after commit's JSRegExp.cs.
"""
import subprocess, sys
kind, tree = sys.argv[1], sys.argv[2]
if kind == "c":
    p = f"{tree}/Broiler.JS/Broiler.JavaScript.BuiltIns/RegExp/JSRegExp.Broiler.cs"
    s = open(p, "rb").read()
    old = (b"        if (status == RegexStatus.LimitExceeded)\r\n"
           b"            throw JSEngine.NewRangeError(limit == RegexLimit.Trail\r\n"
           b"                ? \"Maximum regular expression backtracking trail exceeded\"\r\n"
           b"                : \"Maximum regular expression backtracking frames exceeded\");\r\n")
    new = (b"        if (status == RegexStatus.LimitExceeded)\r\n"
           b"            return RegexMatchData.NoMatch; // control: a ceiling answers no match\r\n")
    assert s.count(old) == 1, "the throw is not found once"
    open(p, "wb").write(s.replace(old, new))
elif kind == "e":
    p = f"{tree}/Broiler.JS/Broiler.JavaScript.BuiltIns/RegExp/JSRegExp.cs"
    base = subprocess.run(["git", "-C", "/home/user/js-115after", "show",
                           "5429c4065e8b273e5c7965598b8a2f5490a8ad98:Broiler.JS/Broiler.JavaScript.BuiltIns/RegExp/JSRegExp.cs"],
                          check=True, capture_output=True).stdout.split(b"\r\n")
    i = [k for k, l in enumerate(base) if l.startswith(b"    private static string BuildFlagsString(")]
    assert len(i) == 1
    j = i[0]
    while base[j] != b"    }":
        j += 1
    method = b"\r\n".join(base[i[0]: j + 1]) + b"\r\n"
    s = open(p, "rb").read()
    tail = b"\r\n}\r\n"
    assert s.endswith(tail) and s.count(b"BuildFlagsString") == 0
    open(p, "wb").write(s[: -len(tail)] + b"\r\n\r\n" + method + b"}\r\n")
else:
    raise SystemExit("c or e")
