// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// German and English plural categories, the dataset slice I4's PluralRules record keeps (JSD-0047).
// Each format selects the category of every value, cardinal and ordinal, under digit options,
// rounding modes, increments, priorities and notations, and of ranges; and writes its resolved
// options. It uses no host function and completes with its lines, so Node and the profile run the
// same text. Non-ASCII is escaped.

var formats = [
  ['en'], ['en-US'], ['de'], ['de-DE'],
  ['en', { type: 'ordinal' }], ['en-US', { type: 'ordinal' }], ['de', { type: 'ordinal' }],
  ['en', { minimumFractionDigits: 1 }], ['de', { minimumFractionDigits: 2 }],
  ['en', { maximumFractionDigits: 0 }], ['en', { type: 'ordinal', maximumFractionDigits: 0 }],
  ['en', { minimumSignificantDigits: 2 }], ['en', { maximumSignificantDigits: 1 }],
  ['de', { maximumSignificantDigits: 2, roundingMode: 'floor' }],
  ['en', { maximumFractionDigits: 0, roundingMode: 'trunc' }], ['en', { maximumFractionDigits: 0, roundingMode: 'halfEven' }],
  ['en', { minimumFractionDigits: 1, maximumFractionDigits: 1, roundingIncrement: 5 }],
  ['en', { minimumFractionDigits: 2, trailingZeroDisplay: 'stripIfInteger' }],
  ['en', { maximumFractionDigits: 1, maximumSignificantDigits: 1, roundingPriority: 'morePrecision' }],
  ['en', { maximumFractionDigits: 1, maximumSignificantDigits: 1, roundingPriority: 'lessPrecision' }],
  ['en', { minimumIntegerDigits: 3 }],
  ['en', { notation: 'compact' }], ['de', { notation: 'compact', compactDisplay: 'long' }],
  ['en', { notation: 'scientific' }], ['de', { notation: 'engineering', type: 'ordinal' }],
  ['en-u-nu-arab'], ['de-u-co-phonebk', { localeMatcher: 'lookup' }]
];

var values = [0, 1, 2, 3, 4, 5, 11, 12, 13, 21, 22, 23, 100, 101, 102, 103, 111, 1000, 1000000, 0.5, 1.5,
  0.95, 0.999, 1.004, 1.05, 2.5, -1, -2, -0, NaN, Infinity, -Infinity, '1.0', '1.00000000000000000001', 1e21, 1n];

var ranges = [[1, 2], [0, 1], [1, 1], [2, 3], [1, 1.0001], [1, 5], [1, 2.5], [0.5, 1], [-1, 1], [NaN, 1], [1, undefined]];

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

function show(value) {
  return typeof value === 'string' ? "'" + value + "'" : typeof value === 'bigint' ? value + 'n' :
    value === 0 && 1 / value < 0 ? '-0' : String(value);
}

function attempt(run) {
  try {
    return run();
  } catch (error) {
    return error.name;
  }
}

var lines = [];

for (var f = 0; f < formats.length; f++) {
  var head = (f + 1) + ' ' + formats[f][0] + (formats[f][1] ? ' ' + JSON.stringify(formats[f][1]) : '');
  var rules = new Intl.PluralRules(formats[f][0], formats[f][1]);

  for (var v = 0; v < values.length; v++) {
    lines.push(head + ' select ' + show(values[v]) + ' => ' + attempt(function () { return rules.select(values[v]); }));
  }

  for (var r = 0; r < ranges.length; r++) {
    lines.push(head + ' selectRange ' + show(ranges[r][0]) + ' ' + show(ranges[r][1]) + ' => ' +
      attempt(function () { return rules.selectRange(ranges[r][0], ranges[r][1]); }));
  }

  lines.push(head + ' resolvedOptions => ' + JSON.stringify(rules.resolvedOptions()));
}

lines.push('call => ' + attempt(function () { return Intl.PluralRules(); }));
lines.push('type => ' + attempt(function () { return new Intl.PluralRules('en', { type: 'plural' }); }));
lines.push('supportedLocalesOf => ' + Intl.PluralRules.supportedLocalesOf(['de-AT', 'en', 'de-DE', 'zz']).join(','));

lines.map(escape).join('\n');
