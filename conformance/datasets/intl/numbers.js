// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// German and English number formatting, the dataset JSD-0027 section 7 asks slice I2 to retain
// (JSD-0044). Each case formats the same values with one Intl.NumberFormat, and writes each answer
// with its parts; ranges are written the same way. The program completes with the lines; it uses no
// host function, so Node and the profile run the same text. Non-ASCII is escaped, so the retained
// answers are ASCII.

var values = [0, -0, 1, -1, 0.5, 1.5, 2.5, -2.5, 0.125, 1.005, 12.345, 999.995, 1000, 1234.5678,
  -1234.5678, 99999, 100000, 123456789, 1e21, 1.23e-7, 5e-324, NaN, Infinity, -Infinity,
  '123456789012345678901234567890.5', '-0.000', '0x1F', ' 42 ', '1e400', '1e-400', 7n, -12345678901234567890n];

var cases = [
  ['en', {}],
  ['en-US', { minimumFractionDigits: 2 }],
  ['en', { maximumFractionDigits: 0 }],
  ['en', { minimumIntegerDigits: 3 }],
  ['en', { maximumSignificantDigits: 3 }],
  ['en', { minimumSignificantDigits: 4 }],
  ['en', { maximumSignificantDigits: 2, maximumFractionDigits: 2, roundingPriority: 'morePrecision' }],
  ['en', { maximumSignificantDigits: 2, maximumFractionDigits: 2, roundingPriority: 'lessPrecision' }],
  ['en', { useGrouping: false }],
  ['en', { useGrouping: 'min2' }],
  ['en', { useGrouping: 'always' }],
  ['en', { signDisplay: 'always' }],
  ['en', { signDisplay: 'exceptZero' }],
  ['en', { signDisplay: 'never' }],
  ['en', { signDisplay: 'negative' }],
  ['en', { roundingMode: 'halfEven', maximumFractionDigits: 1 }],
  ['en', { roundingMode: 'floor', maximumFractionDigits: 1 }],
  ['en', { roundingMode: 'ceil', maximumFractionDigits: 1 }],
  ['en', { roundingMode: 'trunc', maximumFractionDigits: 1 }],
  ['en', { roundingIncrement: 25, minimumFractionDigits: 2, maximumFractionDigits: 2 }],
  ['en', { trailingZeroDisplay: 'stripIfInteger', minimumFractionDigits: 2 }],
  ['en', { style: 'percent' }],
  ['en', { style: 'percent', signDisplay: 'exceptZero', maximumFractionDigits: 1 }],
  ['en', { style: 'currency', currency: 'USD' }],
  ['en', { style: 'currency', currency: 'EUR', currencyDisplay: 'code' }],
  ['en', { style: 'currency', currency: 'EUR', currencyDisplay: 'name' }],
  ['en', { style: 'currency', currency: 'JPY' }],
  ['en', { style: 'currency', currency: 'CAD', currencyDisplay: 'narrowSymbol' }],
  ['en', { style: 'currency', currency: 'USD', currencySign: 'accounting' }],
  ['en', { style: 'currency', currency: 'USD', currencySign: 'accounting', signDisplay: 'always' }],
  ['en', { style: 'currency', currency: 'USD', notation: 'compact' }],
  ['en', { notation: 'scientific' }],
  ['en', { notation: 'engineering', maximumFractionDigits: 2 }],
  ['en', { notation: 'compact' }],
  ['en', { notation: 'compact', compactDisplay: 'long' }],
  ['en', { style: 'unit', unit: 'kilometer-per-hour' }],
  ['en', { style: 'unit', unit: 'liter', unitDisplay: 'long' }],
  ['en', { style: 'unit', unit: 'percent' }],
  ['en', { style: 'unit', unit: 'megabyte', unitDisplay: 'narrow' }],
  ['en-u-nu-thai', {}],
  ['en', { numberingSystem: 'thai' }],
  ['de', {}],
  ['de-DE', { minimumFractionDigits: 2 }],
  ['de', { useGrouping: 'min2' }],
  ['de', { signDisplay: 'exceptZero' }],
  ['de', { style: 'percent' }],
  ['de', { style: 'currency', currency: 'EUR' }],
  ['de', { style: 'currency', currency: 'USD', currencyDisplay: 'name' }],
  ['de', { style: 'currency', currency: 'USD', currencyDisplay: 'code' }],
  ['de', { style: 'currency', currency: 'EUR', currencySign: 'accounting' }],
  ['de', { notation: 'scientific' }],
  ['de', { notation: 'compact' }],
  ['de', { notation: 'compact', compactDisplay: 'long' }],
  ['de', { style: 'currency', currency: 'EUR', notation: 'compact' }],
  ['de', { style: 'unit', unit: 'kilometer-per-hour', unitDisplay: 'long' }],
  ['de', { style: 'unit', unit: 'celsius' }]
];

var ranges = [[1, 5], [1, 1], [-5, -1], [0.9999, 1.0001], [1000, 5000], [3, 5000000], [-1, 1]];

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

function parts(list) {
  var out = '';
  for (var i = 0; i < list.length; i++) {
    out += (i ? ' ' : '') + list[i].type + (list[i].source ? '/' + list[i].source : '') + ':"' + escape(list[i].value) + '"';
  }
  return out;
}

function show(value) {
  return typeof value === 'bigint' ? value + 'n' : typeof value === 'string' ? '"' + value + '"' : Object.is(value, -0) ? '-0' : String(value);
}

var lines = [];

for (var c = 0; c < cases.length; c++) {
  var format = new Intl.NumberFormat(cases[c][0], cases[c][1]);
  var head = (c + 1) + ' ' + cases[c][0] + ' ' + JSON.stringify(cases[c][1]);

  for (var v = 0; v < values.length; v++) {
    lines.push(head + ' ' + show(values[v]) + ' => "' + escape(format.format(values[v])) + '" ' + parts(format.formatToParts(values[v])));
  }

  for (var r = 0; r < ranges.length; r++) {
    lines.push(head + ' range ' + ranges[r][0] + '..' + ranges[r][1] + ' => "' +
      escape(format.formatRange(ranges[r][0], ranges[r][1])) + '" ' + parts(format.formatRangeToParts(ranges[r][0], ranges[r][1])));
  }

  var resolved = format.resolvedOptions();
  var options = [];
  for (var key in resolved) {
    options.push(key + '=' + resolved[key]);
  }
  lines.push(head + ' resolved ' + options.join(' '));
}

lines.push('toLocaleString ' + escape((1234.5).toLocaleString('de')) + ' ' + escape((-0.5).toLocaleString('en', { style: 'percent' })) +
  ' ' + escape((12345678901234567890n).toLocaleString('de-DE')));

lines.join('\n');
