// Bundle sjl-9-001, part C, class V's reading of the Collator's group (divergence-groups.tsv line 50): for each
// case of locales.js whose Locale has no co keyword, the collations this engine's Intl.Collator resolves for it, each
// value c of Intl.supportedValuesOf("collation") for which new Intl.Collator(tag, {collation: c}).resolvedOptions()
// .collation is c, tag being the Locale's toString(), sorted in code-unit order. Written by datasets/classify.py from
// conformance/datasets/intl/locales.js's cases, which it copies.
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

var lines = [];
var values = Intl.supportedValuesOf('collation');
for (var c = 0; c < cases.length; c++) {
  var head = (c + 1) + ' ' + cases[c][0] + (cases[c][1] ? ' ' + JSON.stringify(cases[c][1]) : '');
  try {
    var locale = new Intl.Locale(cases[c][0], cases[c][1]);
    if (locale.collation !== undefined) { lines.push(head + ' collations (co ' + locale.collation + ', not read)'); continue; }
    var tag = locale.toString();
    var list = values.filter(function (v) { return new Intl.Collator(tag, { collation: v }).resolvedOptions().collation === v; });
    list.sort(function (a, b) { return a < b ? -1 : a > b ? 1 : 0; });
    lines.push(head + ' collations => [' + list.join(',') + '] tag=' + tag);
  } catch (error) {
    lines.push(head + ' collations => ' + error.name);
  }
}
console.log('supportedValuesOf(collation) => [' + values.join(',') + ']\n' + lines.map(escape).join('\n'));
