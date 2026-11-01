// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Bundle sjl-17-001, part C of decision-rule.md: collation under invariant globalization (milestone SJL-17,
// work package SJL-17.1). Run in the shell built in Release, `BroilerJS.dll --script-host invariant-probe.js`,
// with DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1, no LANG and TZ=UTC. Each line is one comparison or one sort, and
// names the locale the collator resolved (risk K5): under invariant globalization the host has no culture, so
// the available-locale policy (JSD-0082 decision D7) leaves `en-US` and `en`, and every other request resolves to
// the default, `en-US`. Expected answers: invariant-probe.expected.txt, byte for byte.
// The probe uses only `print`, Intl.Collator and String.prototype.localeCompare. A line whose step throws is
// written `<label> | abort: <error name>`.
var words = ['b', 'B', 'a', 'A', 'ä', 'Ä', 'ae', 'Ae', 'af', 'z', 'é', 'e', 'E', 'ñ', 'n', 'o', 'ö', 'oe',
  'ß', 'ss', 'st', '1', '2', '10', 'a1', 'a2', 'a10', ' ', '-', '_', 'a-b', 'a b', 'ab', 'aB', 'α', 'а', 'å', 'v', 'w'];
function sgn(n) { return n < 0 ? '<' : n > 0 ? '>' : '='; }
function q(s) {
  var r = '';
  for (var i = 0; i < s.length; i++) {
    var c = s.charCodeAt(i);
    r += c < 0x20 || c > 0x7e || c === 0x22 || c === 0x5c ? '\\u' + ('0000' + c.toString(16)).slice(-4) : s.charAt(i);
  }
  return '"' + r + '"';
}
function sorted(c) {
  var w = words.slice().sort(c.compare), s = q(w[0]);
  for (var i = 1; i < w.length; i++) s += ' ' + sgn(c.compare(w[i - 1], w[i])) + ' ' + q(w[i]);
  return s;
}
function line(label, fn) {
  var r;
  try { r = fn(); } catch (e) { r = 'abort: ' + ((e && e.constructor && e.constructor.name) || typeof e); }
  print(label + ' | ' + r);
}
// [label, locales, options]: the covered locales of decision E3 (en, de, de phonebook and search, sv, es, th),
// locales the host lacks (ja, zh, tr, xx, the default), and the four options.
var cases = [
  ['en', 'en', undefined],
  ['en-US', 'en-US', undefined],
  ['de', 'de', undefined],
  ['de-u-co-phonebk', 'de-u-co-phonebk', undefined],
  ['de search', 'de', { usage: 'search' }],
  ['sv', 'sv', undefined],
  ['es', 'es', undefined],
  ['th', 'th', undefined],
  ['ja', 'ja', undefined],
  ['zh', 'zh', undefined],
  ['tr', 'tr', undefined],
  ['xx', 'xx', undefined],
  ['default', undefined, undefined],
  ['en base', 'en', { sensitivity: 'base' }],
  ['en accent', 'en', { sensitivity: 'accent' }],
  ['en case', 'en', { sensitivity: 'case' }],
  ['en ignorePunctuation', 'en', { ignorePunctuation: true }],
  ['en numeric', 'en', { numeric: true }],
  ['en caseFirst upper', 'en', { caseFirst: 'upper' }],
  ['en caseFirst lower', 'en', { caseFirst: 'lower' }],
  ['en-u-kn-kf-upper', 'en-u-kn-kf-upper', undefined],
  ['de numeric ignorePunctuation', 'de', { numeric: true, ignorePunctuation: true }]
];
for (var i = 0; i < cases.length; i++) {
  (function (k) {
    line('sort ' + k[0], function () {
      var c = new Intl.Collator(k[1], k[2]);
      return 'resolved ' + c.resolvedOptions().locale + ' | ' + sorted(c);
    });
  })(cases[i]);
}
var pairs = [['a', 'B'], ['B', 'a'], ['ä', 'b'], ['ä', 'ä'], ['Å', 'Å'], ['a­b', 'ab'], ['\ud800', '\udc00'],
  ['\ud800', 'a'], ['a2', 'a10'], ['', ' '], ['A', 'a'], ['ß', 'ss']];
for (var j = 0; j < pairs.length; j++) {
  (function (p) {
    line('localeCompare ' + q(p[0]) + ' ' + q(p[1]), function () {
      return 'resolved ' + new Intl.Collator().resolvedOptions().locale + ' | ' + sgn(p[0].localeCompare(p[1]));
    });
    line('localeCompare de ' + q(p[0]) + ' ' + q(p[1]), function () {
      return 'resolved ' + new Intl.Collator('de').resolvedOptions().locale + ' | ' + sgn(p[0].localeCompare(p[1], 'de'));
    });
  })(pairs[j]);
}
