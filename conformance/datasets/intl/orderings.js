// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// German and English orderings, the dataset JSD-0027 section 7 asks slice I1 to retain (JSD-0043).
// Each case sorts the same words with one collator and writes them in order, with `=` between two
// the collator calls equal and `<` otherwise. The program completes with the lines; it uses no host
// function, so Node and the profile run the same text. Non-ASCII is escaped both ways, so the
// retained answers are ASCII.

var words = [
  'a', 'A', 'ä', 'Ä', 'ae', 'Ae', 'AE', 'af', 'Af', 'b', 'B',
  'Apfel', 'Äpfel', 'Affe', 'Aepfel',
  'o', 'ö', 'oe', 'Ofen', 'Öl', 'Oel', 'offen', 'öffnen',
  'u', 'ü', 'ue', 'Übel', 'Uebel', 'Ufer',
  'ss', 'ß', 'st', 'Straße', 'Strasse', 'Strasze', 'straße',
  'co-op', 'coop', 'co op', 'Coop', 'co_op',
  'cote', 'coté', 'côte', 'côté',
  'resume', 'résumé', 'Resume', 'Résumé',
  'naive', 'naïve', 'éclair', 'Eclair', 'eclair',
  '1', '2', '10', '100', 'a1', 'a2', 'a10', 'file-2', 'file10', 'file 3',
  'Zebra', 'zebra', 'zoo', 'Ångström', 'Angstrom',
  'ﬁle', 'file', 'éclair', 'Ⅳ', 'IV', 'iv',
  '', ' ', '-', '!', '#hash', 'hash'
];

var cases = [
  ['en', {}],
  ['en-US', {}],
  ['en', { sensitivity: 'base' }],
  ['en', { sensitivity: 'accent' }],
  ['en', { sensitivity: 'case' }],
  ['en', { ignorePunctuation: true }],
  ['en', { numeric: true }],
  ['en', { caseFirst: 'upper' }],
  ['en', { caseFirst: 'lower' }],
  ['en-u-kn-kf-upper', {}],
  ['en', { usage: 'search' }],
  ['en', { usage: 'search', sensitivity: 'base' }],
  ['de', {}],
  ['de-DE', { sensitivity: 'base' }],
  ['de', { sensitivity: 'accent' }],
  ['de', { caseFirst: 'upper' }],
  ['de', { numeric: true, ignorePunctuation: true }],
  ['de-u-co-phonebk', {}],
  ['de-u-co-phonebk', { sensitivity: 'base' }],
  ['de-DE-u-co-phonebk', { caseFirst: 'upper' }],
  ['de-u-co-phonebk', { sensitivity: 'case' }],
  ['de-u-co-phonebk', { caseFirst: 'lower' }],
  ['de', { usage: 'search' }],
  ['de', { usage: 'search', sensitivity: 'base' }],
  ['de', { usage: 'search', sensitivity: 'case' }],
  ['de', { usage: 'search', caseFirst: 'upper' }],
  ['en', { sensitivity: 'case', caseFirst: 'upper' }]
];

function escape(text) {
  var out = '';
  for (var i = 0; i < text.length; i++) {
    var unit = text.charCodeAt(i);
    out += unit < 0x20 || unit > 0x7e || text[i] === '\\'
      ? '\\u' + ('000' + unit.toString(16)).slice(-4)
      : text[i];
  }
  return out;
}

function quote(text) {
  return '"' + escape(text) + '"';
}

var lines = [];

for (var c = 0; c < cases.length; c++) {
  var collator = new Intl.Collator(cases[c][0], cases[c][1]);
  var sorted = words.slice().sort(collator.compare);
  var line = (c + 1) + ' ' + cases[c][0] + ' ' + JSON.stringify(cases[c][1]) + ' ' + quote(sorted[0]);

  for (var w = 1; w < sorted.length; w++) {
    line += (collator.compare(sorted[w - 1], sorted[w]) === 0 ? ' = ' : ' < ') + quote(sorted[w]);
  }

  lines.push(line);
}

lines.join('\n');
