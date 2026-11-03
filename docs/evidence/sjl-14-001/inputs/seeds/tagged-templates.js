// SJL-14.1, part J seed: templates and tagged templates, raw and cooked strings, an invalid escape in a tagged template
// (K3; the bridge's CookedText and CookedInvalid facts; decision D44).
function tag(strings) { return strings.raw.join('|') + '/' + strings.map(function (s) { return s === undefined ? 'u' : s; }).join('|'); }
var a = 1, b = 2;
var t1 = tag`x${a}}}y${b}}z`;
var t2 = tag`\unicode and \u{110000} and \x`;
var t3 = `nested ${`inner ${a + b}`} done`;
var t4 = String.raw`\n${a}\t`;
[t1, t2, t3, t4].join(' ; ');
