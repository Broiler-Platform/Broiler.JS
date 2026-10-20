#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Instrument a scratch copy of Broiler.VM's compiler so that every diagnostic records the source line that made it
(SJL-12.3 task 3.11's cross-check; never applied to a checkout that is committed from).

    git -C <Broiler.VM> archive d82b07a Directory.Build.props Directory.Packages.props NuGet.config eng/packages \
        eng/Broiler.Packaging.props eng/nuget src/Broiler.VM.Abstractions src/Broiler.VM.Profile.JavaScript.Format \
        src/Broiler.VM.Profile.JavaScript.Compiler | tar -x -C <scratch>
    python3 -I instrument.py <scratch>/src/Broiler.VM.Profile.JavaScript.Compiler

Every edit replaces text within one line, so every line number of the copy is the commit's. The probe beside this file
(Program.cs, built with a project that references the instrumented compiler) prints each diagnostic with its stack.
"""
import sys

PROBE = '''// Scratch instrumentation for SJL-12.3 task 3.11 (never committed): records where each diagnostic was made.
namespace Broiler.VM.Profile.JavaScript.Compiler;

public static class ProbeSites
{
    public static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SliceSourceDiagnostic, string> Where = new();
    [System.ThreadStatic] public static string LastRegExp;

    public static SliceSourceDiagnostic Tag(SliceSourceDiagnostic d)
    {
        var st = new System.Diagnostics.StackTrace(1, true);
        var sb = new System.Text.StringBuilder();
        foreach (var f in st.GetFrames())
        {
            var fn = f.GetFileName();
            if (fn == null) continue;
            sb.Append(System.IO.Path.GetFileName(fn)).Append(':').Append(f.GetFileLineNumber()).Append(' ');
            if (sb.Length > 600) break;
        }
        if (LastRegExp != null) { sb.Append("| regexp ").Append(LastRegExp); LastRegExp = null; }
        Where.AddOrUpdate(d, sb.ToString().Trim());
        return d;
    }

    public static void RegExp(System.Exception failure)
    {
        var st = new System.Diagnostics.StackTrace(failure, true);
        var f = st.FrameCount > 0 ? st.GetFrame(0) : null;
        LastRegExp = f?.GetFileName() == null ? "?" : System.IO.Path.GetFileName(f.GetFileName()) + ":" + f.GetFileLineNumber();
    }
}
'''

ADD = 'diagnostics.Add(new SliceSourceDiagnostic('
TAGGED = 'diagnostics.Add(ProbeSites.Tag(new SliceSourceDiagnostic('
EDITS = {
    'JsCompiler.Emission.cs': [(251, ADD, TAGGED), (251, 'span.Column));', 'span.Column)));')],
    'JsParser.cs': [(7423, ADD, TAGGED), (7423, 'span.Column));', 'span.Column)));')],
    'SliceParser.cs': [(1876, ADD, TAGGED), (1876, 'Current.Column));', 'Current.Column)));')],
    'SliceSourceCompiler.cs': [(1028, ADD, TAGGED), (1028, 'span.Column));', 'span.Column)));')],
    'SliceStaticSemantics.cs': [(829, ADD, TAGGED), (829, 'span.Column));', 'span.Column)));')],
    'SliceTokenizer.cs': [(2455, ADD, TAGGED), (2455, 'atColumn));', 'atColumn)));')],
    'JsNumericAdmission.cs': [(104, ADD, TAGGED), (108, 'span.Column));', 'span.Column)));')],
    'JsCompiler.Literals.cs': [(447, 'Refuse(pattern.Span,', 'ProbeSites.RegExp(failure); Refuse(pattern.Span,')],
}


def main(cdir):
    for name, edits in EDITS.items():
        path = f'{cdir}/{name}'
        with open(path, encoding='utf-8') as f:
            lines = f.read().split('\n')
        for number, old, new in edits:
            if lines[number - 1].count(old) != 1:
                sys.exit(f'{name}:{number} does not hold {old!r} once')
            lines[number - 1] = lines[number - 1].replace(old, new)
        with open(path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(lines))
    with open(f'{cdir}/ProbeSites.cs', 'w', encoding='utf-8') as f:
        f.write(PROBE)
    print('instrumented', len(EDITS), 'files')


if __name__ == '__main__':
    main(sys.argv[1])
