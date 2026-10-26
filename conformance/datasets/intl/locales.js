// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Intl.Locale over tags of every shape, the dataset slice I4's Locale record keeps (JSD-0046). Each
// case makes a Locale from a tag and options, and writes its identifier and parts, its keywords,
// its maximal and minimal forms, and what its information methods answer. Locale reads CLDR's
// likely subtags and aliases for every language, not only the supported ones, so the tags range
// over scripts, regions, variants, extensions and deprecated forms. It uses no host function and
// completes with its lines, so Node and the profile run the same text. Non-ASCII is escaped.

var cases = [
  ['en'], ['en-US'], ['en-GB'], ['en-Latn'], ['en-Arab'], ['en-Shaw'], ['de'], ['de-AT'], ['de-CH'],
  ['fr'], ['fr-CA'], ['es-419'], ['pt-BR'], ['ar'], ['ar-EG'], ['he'], ['fa-IR'], ['ur'], ['hi'],
  ['zh'], ['zh-Hant'], ['zh-TW'], ['zh-Hans-SG'], ['ja'], ['ko'], ['th'], ['ru'], ['sr-Latn'], ['uz-Cyrl'],
  ['und'], ['und-AQ'], ['und-Latn-AQ'], ['und-Cyrl-RO'], ['und-Thai'], ['und-150'], ['und-AT'],
  ['mo'], ['iw'], ['in-ID'], ['sh'], ['no'], ['tl'], ['art-lojban'], ['zh-hakka'], ['aar-x-private'],
  ['es-ES-preeuro'], ['sl-rozaj-biske-1994'], ['hy-SU'], ['ru-SU'], ['en-u-ca-gregory-co-phonebk-kn'],
  ['en-u-nu-thai-hc-h23'], ['de-u-co-phonebk-ca-buddhist'], ['en-u-fw-mon'], ['en-u-rg-gbzzzz'],
  ['en-u-sd-usca'], ['ar-u-nu-arab'], ['pt-u-attr2-attr1-ca-gregory'], ['en-t-iw'], ['x-private'.replace('x-', 'und-x-')],
  ['en', { calendar: 'islamicc', collation: 'emoji', hourCycle: 'h11', caseFirst: 'upper', numeric: true, numberingSystem: 'arab', firstDayOfWeek: '0' }],
  ['en-US', { language: 'fr', script: 'Cyrl', region: 'BE', variants: 'Fonipa-1996' }],
  ['de-u-kn-false', { numeric: true }],
  ['en', { firstDayOfWeek: 'tue' }],
  ['ja-JP', { region: 'US' }],
  ['he-IL', { script: 'Latn' }]
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

// THE INFORMATION IS READ IN EITHER FORM: the current draft's methods (getWeekInfo), or the
// accessors (weekInfo) of the proposal stage Node 22 implements.
function info(locale, name) {
  var method = locale['get' + name[0].toUpperCase() + name.slice(1)];
  return typeof method === 'function' ? method.call(locale) : locale[name];
}

function show(value) {
  return value === undefined ? 'undefined' : Array.isArray(value) ? '[' + value.join(',') + ']' : String(value);
}

var lines = [];

for (var c = 0; c < cases.length; c++) {
  var head = (c + 1) + ' ' + cases[c][0] + (cases[c][1] ? ' ' + JSON.stringify(cases[c][1]) : '');

  try {
    var locale = new Intl.Locale(cases[c][0], cases[c][1]);
    var week = info(locale, 'weekInfo');
    lines.push(head + ' => ' + locale + ' base=' + locale.baseName + ' language=' + show(locale.language) +
      ' script=' + show(locale.script) + ' region=' + show(locale.region));
    lines.push(head + ' variants => ' + show(locale.variants));
    lines.push(head + ' keywords => calendar=' + show(locale.calendar) + ' collation=' + show(locale.collation) +
      ' hourCycle=' + show(locale.hourCycle) + ' caseFirst=' + show(locale.caseFirst) +
      ' numeric=' + locale.numeric + ' numberingSystem=' + show(locale.numberingSystem));
    lines.push(head + ' firstDayOfWeek => ' + show(locale.firstDayOfWeek));
    lines.push(head + ' likely => max=' + locale.maximize() + ' min=' + locale.minimize());
    lines.push(head + ' calendars => ' + show(info(locale, 'calendars')));
    lines.push(head + ' collations => ' + show(info(locale, 'collations')));
    lines.push(head + ' hourCycles => ' + show(info(locale, 'hourCycles')));
    lines.push(head + ' numberingSystems => ' + show(info(locale, 'numberingSystems')));
    lines.push(head + ' timeZones => ' + show(info(locale, 'timeZones')));
    lines.push(head + ' direction => ' + show(info(locale, 'textInfo').direction));
    lines.push(head + ' week => firstDay=' + week.firstDay + ' weekend=' + show(week.weekend));
  } catch (error) {
    lines.push(head + ' => ' + error.name);
  }
}

lines.map(escape).join('\n');
