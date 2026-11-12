// The fixer's probe on branch js-c78: the values the reviewers named (negative zero, an exactly built value, the default
// rounding), and the m literal's and other sites' host-culture reads, each m literal compiled inside eval so that a parse
// refusal under one culture is that line's answer and not the run's end.
function line(label, f) { var r; try { r = String(f()); } catch (e) { r = "throws " + (e && e.name) + ": " + (e && e.message); } console.log(label + " => " + r); }
line("defaultLocale", function () { return new Intl.NumberFormat().resolvedOptions().locale; });
line("exact = Decimal('1234567891') / 1000m; exact.toString()", function () { return (Decimal('1234567891') / 1000m).toString(); });
line("exact.toLocaleString()", function () { return (Decimal('1234567891') / 1000m).toLocaleString(); });
line("exact.toLocaleString('en')", function () { return (Decimal('1234567891') / 1000m).toLocaleString('en'); });
line("(Decimal('12345678901234567890123456789') / 1000000000m).toLocaleString('en')", function () { return (Decimal('12345678901234567890123456789') / 1000000000m).toLocaleString('en'); });
line("(-0m).toLocaleString('en')", function () { return eval("(-0m).toLocaleString('en')"); });
line("(0m * -1m).toLocaleString('en')", function () { return eval("(0m * -1m).toLocaleString('en')"); });
line("(-0m).toLocaleString()", function () { return eval("(-0m).toLocaleString()"); });
line("(-0).toLocaleString('en')", function () { return (-0).toLocaleString('en'); });
line("(1234567.891m).toString()", function () { return eval("(1234567.891m).toString()"); });
line("(1.5m).toFixed(1)", function () { return eval("(1.5m).toFixed(1)"); });
line("(-0.5m).toString()", function () { return eval("(-0.5m).toString()"); });
line("Decimal('\\u22125').toString()", function () { return Decimal('−5').toString(); });
line("(-5).toString()", function () { return (-5).toString(); });
line("String(-1.5)", function () { return String(-1.5); });
line("(-5n).toString()", function () { return (-5n).toString(); });
line("'I'.toLowerCase()", function () { return 'I'.toLowerCase(); });
line("Object.keys(JSON.parse('{\"1\":1,\"-1\":2,\"01\":3}'))", function () { return JSON.stringify(Object.keys(JSON.parse('{"1":1,"-1":2,"01":3}'))); });
line("/a{0,2}/.exec('aa')", function () { return /a{0,2}/.exec('aa')[0]; });
