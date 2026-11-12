// The error messages and matches the changelog entry of 2026-11-12 names (bundle sjl-9-001, readings-2026-11-12):
// each line's text, or the error it throws, under the host culture the run sets.
function line(label, f) { var r; try { r = String(f()); } catch (e) { r = "throws " + (e && e.name) + ": " + (e && e.message); } console.log(label + " => " + r); }
line("String.fromCodePoint(1.5)", function () { return String.fromCodePoint(1.5); });
line("var n = 1.5; n()", function () { var n = 1.5; return n(); });
line("new Uint16Array(new ArrayBuffer(8), 3)", function () { return new Uint16Array(new ArrayBuffer(8), 3); });
line("new DataView(new ArrayBuffer(2)).getInt32(0)", function () { return new DataView(new ArrayBuffer(2)).getInt32(0); });
line("/I/i.test('i'), /i/i.test('\\u0130'), /I/iu.test('i')", function () { return /I/i.test('i') + "," + /i/i.test('İ') + "," + /I/iu.test('i'); });
