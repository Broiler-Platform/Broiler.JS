// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// The driver of run_reference_datasets.py, the harness of bundle sjl-8-001's rule, part C (milestone
// SJL-8.4). For each program the harness writes one copy of this file, with the program's file name as a
// JSON string where the marker below stands, and runs it as `dotnet BroilerJS.dll --script-host <copy>` in
// the directory that holds conformance/datasets/time/'s copies, with TZ=UTC and no LANG.
//
// It does what Broiler.VM's reference command does in Node, `(0, eval)(fs.readFileSync(file, 'utf8'))`:
// it reads the program's text with the shell's `read` and evaluates it by indirect eval, so the program
// runs as a global script in the realm the shell made, and nothing of this driver is in its scope. Then it
// prints the program's completion value.
//
// The value is printed on one line, each UTF-16 code unit outside printable ASCII, and the backslash,
// written as \uXXXX in lower case. No console encoding can then change a character: the shell's console
// writes UTF-8 when LANG is unset, but it writes a lone surrogate as U+FFFD. The harness reverses the
// escapes, checks the result against the length printed beside it, and splits it into lines itself, as
// Broiler.VM's check splits its own engine's completion value.
//
// What it prints, one line each:
//   reference-dataset-driver 1 <file> <length of the program's text in UTF-16 code units>
//   reference-dataset-completion <typeof the value> <length of String(value)> <String(value), escaped>
// or, if evaluating the program or converting its value threw,
//   reference-dataset-uncaught <the error's name and message, escaped>
(function (read, print) {
  function ascii(s) {
    return s.replace(/[^\x20-\x5b\x5d-\x7e]/g, function (c) {
      var hex = c.charCodeAt(0).toString(16);
      return '\\u' + '0000'.slice(hex.length) + hex;
    });
  }

  var file = /*PROGRAM_FILE*/;
  var text = read(file);
  print('reference-dataset-driver 1 ' + file + ' ' + text.length);

  var kind;
  var value;
  try {
    var completion = (0, eval)(text);
    kind = typeof completion;
    value = String(completion);
  } catch (error) {
    var described;
    try {
      described = String(error && error.name) + ': ' + String(error && error.message);
    } catch (unprintable) {
      described = '(an error that could not be described)';
    }
    print('reference-dataset-uncaught ' + ascii(described));
    return;
  }

  print('reference-dataset-completion ' + kind + ' ' + value.length + ' ' + ascii(value));
})(read, print);
